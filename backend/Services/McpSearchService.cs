using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public interface IMcpSearchService
{
    Task<McpSearchResponse> SearchAsync(McpSearchRequest request, int userId, CancellationToken cancellationToken = default);
}

public class McpSearchService : IMcpSearchService
{
    private readonly AppDbContext _context;

    public McpSearchService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<McpSearchResponse> SearchAsync(McpSearchRequest request, int userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException("查询内容不能为空。", nameof(request));
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
            .Where(p => p.UserId == userId);

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

        return new McpSearchResponse
        {
            Total = total,
            Results = results
        };
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

        var details = BuildDetailParagraph(photo, tags);

        return new McpSearchResult
        {
            DocumentId = $"photo-{photo.Id}",
            Score = score,
            Content = new[]
            {
                new McpContentBlock { Type = "text", Text = summary },
                new McpContentBlock { Type = "text", Text = details }
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

    private static string BuildDetailParagraph(Photo photo, IReadOnlyCollection<string> tags)
    {
        var detailBuilder = new StringBuilder();
        if (photo.TakenAt.HasValue)
        {
            detailBuilder.AppendLine($"拍摄时间：{photo.TakenAt:yyyy-MM-dd HH:mm}");
        }

        if (!string.IsNullOrWhiteSpace(photo.Location))
        {
            detailBuilder.AppendLine($"地点：{photo.Location}");
        }

        if (tags.Count > 0)
        {
            detailBuilder.AppendLine($"标签：{string.Join(", ", tags)}");
        }

        detailBuilder.AppendLine($"尺寸：{photo.Width}x{photo.Height}");
        return detailBuilder.ToString().Trim();
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
