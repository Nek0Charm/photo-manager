using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class McpController : ControllerBase
    {
        private readonly AppDbContext _context;

        public McpController(AppDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] McpSearchRequest request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { message = "请输入查询内容" });
            }

            var normalizedQuery = request.Query.Trim();
            var normalizedTokens = normalizedQuery
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(token => token.ToLowerInvariant())
                .ToArray();

            var limit = Math.Clamp(request.Limit, 1, 20);
            var likePattern = $"%{normalizedQuery}%";

            var photoQuery = _context.Photos
                .AsNoTracking()
                .Include(p => p.PhotoTags)
                .ThenInclude(pt => pt.Tag)
                .Where(p => p.UserId == userId.Value);

            if (request.From.HasValue)
            {
                photoQuery = photoQuery.Where(p => (p.TakenAt ?? p.CreatedAt) >= request.From.Value);
            }

            if (request.To.HasValue)
            {
                photoQuery = photoQuery.Where(p => (p.TakenAt ?? p.CreatedAt) <= request.To.Value);
            }

            photoQuery = photoQuery.Where(p =>
                (p.Description != null && EF.Functions.Like(p.Description, likePattern)) ||
                (p.Location != null && EF.Functions.Like(p.Location, likePattern)) ||
                EF.Functions.Like(p.FilePath, likePattern) ||
                p.PhotoTags.Any(pt => pt.Tag != null && EF.Functions.Like(pt.Tag.Name, likePattern)));

            var total = await photoQuery.CountAsync(cancellationToken);

            var candidateLimit = Math.Clamp(limit * 4, limit, 60);
            var candidates = await photoQuery
                .OrderByDescending(p => p.TakenAt ?? p.CreatedAt)
                .Take(candidateLimit)
                .ToListAsync(cancellationToken);

            var results = candidates
                .Select(photo => new
                {
                    Photo = photo,
                    Result = BuildResult(photo, normalizedTokens, normalizedQuery)
                })
                .OrderByDescending(x => x.Result.Score)
                .ThenByDescending(x => x.Photo.TakenAt ?? x.Photo.CreatedAt)
                .Select(x => x.Result)
                .Take(limit)
                .ToList();

            return Ok(new McpSearchResponse
            {
                Total = total,
                Results = results
            });
        }

        private static McpSearchResult BuildResult(Photo photo, IReadOnlyCollection<string> tokens, string fallbackQuery)
        {
            var tags = photo.PhotoTags
                .Where(pt => pt.Tag != null)
                .Select(pt => pt.Tag!.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var corpus = BuildCorpus(photo, tags);
            var score = CalculateScore(corpus, tokens);
            if (score <= 0 && tokens.Count > 0)
            {
                score = corpus.Contains(fallbackQuery, StringComparison.OrdinalIgnoreCase) ? 0.5 : 0.0;
            }

            var summary = !string.IsNullOrWhiteSpace(photo.Description)
                ? photo.Description!
                : $"包含标签：{(tags.Length == 0 ? "暂无" : string.Join(", ", tags))}";

            var detailBuilder = new StringBuilder();
            if (photo.TakenAt.HasValue)
            {
                detailBuilder.AppendLine($"拍摄时间：{photo.TakenAt:yyyy-MM-dd HH:mm}");
            }

            if (!string.IsNullOrWhiteSpace(photo.Location))
            {
                detailBuilder.AppendLine($"地点：{photo.Location}");
            }

            if (tags.Length > 0)
            {
                detailBuilder.AppendLine($"标签：{string.Join(", ", tags)}");
            }

            detailBuilder.AppendLine($"尺寸：{photo.Width}x{photo.Height}");

            return new McpSearchResult
            {
                DocumentId = $"photo-{photo.Id}",
                Score = score,
                Content = new[]
                {
                    new McpContentBlock { Type = "text", Text = summary },
                    new McpContentBlock
                    {
                        Type = "text",
                        Text = detailBuilder.ToString().Trim()
                    }
                },
                Metadata = new Dictionary<string, object?>
                {
                    ["photoId"] = photo.Id,
                    ["fileUrl"] = photo.FilePath,
                    ["thumbnailUrl"] = photo.ThumbnailPath,
                    ["tags"] = tags,
                    ["takenAt"] = photo.TakenAt?.ToString("o"),
                    ["location"] = photo.Location,
                    ["width"] = photo.Width,
                    ["height"] = photo.Height,
                    ["createdAt"] = photo.CreatedAt.ToString("o")
                }
            };
        }

        private static string BuildCorpus(Photo photo, IReadOnlyCollection<string> tags)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(photo.Description))
            {
                builder.Append(' ').Append(photo.Description);
            }

            if (!string.IsNullOrWhiteSpace(photo.Location))
            {
                builder.Append(' ').Append(photo.Location);
            }

            builder.Append(' ').Append(Path.GetFileNameWithoutExtension(photo.FilePath));

            foreach (var tag in tags)
            {
                builder.Append(' ').Append(tag);
            }

            return builder.ToString();
        }

        private static double CalculateScore(string corpus, IReadOnlyCollection<string> tokens)
        {
            if (tokens.Count == 0)
            {
                return 1d;
            }

            var normalizedCorpus = corpus.ToLowerInvariant();
            var matches = tokens.Count(token => normalizedCorpus.Contains(token));
            return (double)matches / tokens.Count;
        }
    }
}
