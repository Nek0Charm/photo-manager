using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IImageService _imageService;
        private readonly IWebHostEnvironment _environment;

        public PhotosController(AppDbContext context, IImageService imageService, IWebHostEnvironment environment)
        {
            _context = context;
            _imageService = imageService;
            _environment = environment;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

        [HttpPost("upload")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> Upload([FromForm] PhotoUploadRequest request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            if (request.File == null)
            {
                return BadRequest(new { message = "请选择要上传的图片" });
            }

            var processingResult = await _imageService.SavePhotoAsync(request.File, cancellationToken);

            var photo = new Photo
            {
                UserId = userId.Value,
                FilePath = processingResult.FilePath,
                ThumbnailPath = processingResult.ThumbnailPath,
                Width = processingResult.Width,
                Height = processingResult.Height,
                TakenAt = request.TakenAt ?? processingResult.TakenAt,
                Location = string.IsNullOrWhiteSpace(request.Location) ? processingResult.Location : request.Location,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var tagName in ParseTags(request.Tags))
            {
                await AttachTagAsync(photo, tagName, TagType.Manual, cancellationToken);
            }

            foreach (var tagName in processingResult.ExifTags)
            {
                await AttachTagAsync(photo, tagName, TagType.Exif, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(photo.Location))
            {
                await AttachTagAsync(photo, photo.Location, TagType.Exif, cancellationToken);
            }

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToDto(photo));
        }

        [HttpPost("list")]
        public async Task<IActionResult> List([FromBody] PhotoListRequest request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            var query = _context.Photos
                .AsNoTracking()
                .Include(p => p.PhotoTags)
                .ThenInclude(pt => pt.Tag)
                .Where(p => p.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(request.Tag))
            {
                var tag = request.Tag.Trim();
                query = query.Where(p => p.PhotoTags.Any(pt => pt.Tag != null && pt.Tag.Name == tag));
            }

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                query = query.Where(p =>
                    (p.Description != null && EF.Functions.Like(p.Description, $"%{keyword}%")) ||
                    EF.Functions.Like(p.FilePath, $"%{keyword}%"));
            }

            if (request.From.HasValue)
            {
                query = query.Where(p => (p.TakenAt ?? p.CreatedAt) >= request.From.Value);
            }

            if (request.To.HasValue)
            {
                query = query.Where(p => (p.TakenAt ?? p.CreatedAt) <= request.To.Value);
            }

            var total = await query.CountAsync(cancellationToken);

            var sortKey = request.Sort?.Trim().ToLowerInvariant();
            query = sortKey switch
            {
                "createdasc" => query.OrderBy(p => p.CreatedAt),
                "takendesc" => query.OrderByDescending(p => p.TakenAt ?? p.CreatedAt),
                "takenasc" => query.OrderBy(p => p.TakenAt ?? p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var photos = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = photos.Select(ToDto).ToList();

            return Ok(new PhotoListResponse
            {
                Total = total,
                Items = items
            });
        }

        [HttpPost("detail")]
        public async Task<IActionResult> Detail([FromBody] PhotoDetailRequest request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            var photo = await _context.Photos
                .AsNoTracking()
                .Include(p => p.PhotoTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == request.Id && p.UserId == userId.Value, cancellationToken);

            if (photo == null)
            {
                return NotFound(new { message = "图片不存在" });
            }

            return Ok(ToDto(photo));
        }

        [HttpPost("edit")]
        public async Task<IActionResult> Edit([FromForm] PhotoEditRequest request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            if (request.File == null)
            {
                return BadRequest(new { message = "请提供编辑后的图片" });
            }

            var photo = await _context.Photos
                .Include(p => p.PhotoTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == request.PhotoId && p.UserId == userId.Value, cancellationToken);

            if (photo == null)
            {
                return NotFound(new { message = "图片不存在" });
            }

            var result = await _imageService.SavePhotoAsync(request.File, cancellationToken);

            if (request.SaveAsNew)
            {
                var newPhoto = new Photo
                {
                    UserId = userId.Value,
                    FilePath = result.FilePath,
                    ThumbnailPath = result.ThumbnailPath,
                    Width = result.Width,
                    Height = result.Height,
                    TakenAt = request.TakenAt ?? result.TakenAt ?? photo.TakenAt,
                    Location = request.Location ?? result.Location ?? photo.Location,
                    Description = request.Description ?? photo.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Photos.Add(newPhoto);

                var manualTags = request.Tags != null
                    ? ParseTags(request.Tags)
                    : photo.PhotoTags
                        .Where(pt => pt.Tag != null && pt.Tag.Type == TagType.Manual)
                        .Select(pt => pt.Tag!.Name)
                        .ToArray();

                foreach (var tag in manualTags)
                {
                    await AttachTagAsync(newPhoto, tag, TagType.Manual, cancellationToken);
                }

                foreach (var tag in result.ExifTags)
                {
                    await AttachTagAsync(newPhoto, tag, TagType.Exif, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(newPhoto.Location))
                {
                    await AttachTagAsync(newPhoto, newPhoto.Location, TagType.Exif, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);

                return Ok(ToDto(newPhoto));
            }

            DeletePhysicalFile(photo.FilePath);
            DeletePhysicalFile(photo.ThumbnailPath);

            photo.FilePath = result.FilePath;
            photo.ThumbnailPath = result.ThumbnailPath;
            photo.Width = result.Width;
            photo.Height = result.Height;
            photo.TakenAt = request.TakenAt ?? result.TakenAt ?? photo.TakenAt;

            if (request.Location != null)
            {
                photo.Location = request.Location;
            }
            else if (!string.IsNullOrWhiteSpace(result.Location))
            {
                photo.Location = result.Location;
            }

            if (request.Description != null)
            {
                photo.Description = request.Description;
            }

            RemoveTags(photo, TagType.Exif);
            foreach (var tag in result.ExifTags)
            {
                await AttachTagAsync(photo, tag, TagType.Exif, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(photo.Location))
            {
                await AttachTagAsync(photo, photo.Location, TagType.Exif, cancellationToken);
            }

            if (request.Tags != null)
            {
                RemoveTags(photo, TagType.Manual);
                foreach (var tag in ParseTags(request.Tags))
                {
                    await AttachTagAsync(photo, tag, TagType.Manual, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToDto(photo));
        }

        [HttpPost("update-metadata")]
        public async Task<IActionResult> UpdateMetadata([FromBody] PhotoMetadataUpdateRequest request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            var photo = await _context.Photos
                .Include(p => p.PhotoTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == request.PhotoId && p.UserId == userId.Value, cancellationToken);

            if (photo == null)
            {
                return NotFound(new { message = "图片不存在" });
            }

            if (request.Description != null)
            {
                photo.Description = request.Description;
            }

            if (request.Tags != null)
            {
                RemoveTags(photo, TagType.Manual);
                foreach (var tag in ParseTags(request.Tags))
                {
                    await AttachTagAsync(photo, tag, TagType.Manual, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToDto(photo));
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] PhotoDeleteRequest request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            var photo = await _context.Photos
                .Include(p => p.PhotoTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == request.PhotoId && p.UserId == userId.Value, cancellationToken);

            if (photo == null)
            {
                return NotFound(new { message = "图片不存在" });
            }

            DeletePhysicalFile(photo.FilePath);
            DeletePhysicalFile(photo.ThumbnailPath);

            if (photo.PhotoTags.Any())
            {
                _context.PhotoTags.RemoveRange(photo.PhotoTags);
            }

            _context.Photos.Remove(photo);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true });
        }

        private async Task AttachTagAsync(Photo photo, string? name, TagType type, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var normalized = name.Trim();
            if (photo.PhotoTags.Any(pt => pt.Tag != null && pt.Tag.Name == normalized && pt.Tag.Type == type))
            {
                return;
            }

            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == normalized && t.Type == type, cancellationToken);
            if (tag == null)
            {
                tag = new Tag { Name = normalized, Type = type };
                _context.Tags.Add(tag);
            }

            photo.PhotoTags.Add(new PhotoTag { Photo = photo, Tag = tag });
        }

        private void RemoveTags(Photo photo, TagType type)
        {
            var targets = photo.PhotoTags
                .Where(pt => pt.Tag != null && pt.Tag.Type == type)
                .ToList();

            foreach (var item in targets)
            {
                _context.PhotoTags.Remove(item);
                photo.PhotoTags.Remove(item);
            }
        }

        private void DeletePhysicalFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var root = ResolveWebRoot();
            var sanitized = relativePath.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);
            var target = Path.Combine(root, sanitized);

            if (System.IO.File.Exists(target))
            {
                System.IO.File.Delete(target);
            }
        }

        private string ResolveWebRoot()
        {
            if (!string.IsNullOrWhiteSpace(_environment.WebRootPath))
            {
                return _environment.WebRootPath!;
            }

            var fallback = Path.Combine(_environment.ContentRootPath ?? AppContext.BaseDirectory, "wwwroot");
            Directory.CreateDirectory(fallback);
            _environment.WebRootPath = fallback;
            return fallback;
        }

        private static PhotoItemDto ToDto(Photo photo)
        {
            return new PhotoItemDto
            {
                Id = photo.Id,
                FileUrl = photo.FilePath,
                ThumbnailUrl = photo.ThumbnailPath,
                Width = photo.Width,
                Height = photo.Height,
                TakenAt = photo.TakenAt,
                Location = photo.Location,
                Description = photo.Description,
                Tags = photo.PhotoTags
                    .Where(pt => pt.Tag != null)
                    .Select(pt => pt.Tag!.Name)
                    .Distinct()
                    .ToArray(),
                CreatedAt = photo.CreatedAt
            };
        }

        private static IEnumerable<string> ParseTags(IEnumerable<string>? tags)
        {
            if (tags == null)
            {
                return Array.Empty<string>();
            }

            return tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim());
        }

        private static IEnumerable<string> ParseTags(string? tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return Array.Empty<string>();
            }

            return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
