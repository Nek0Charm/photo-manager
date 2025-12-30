using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace Backend.Mcp;

[McpServerToolType]
public class PhotoInsightTools
{
    private readonly AppDbContext _dbContext;
    private readonly IMcpSearchService _mcpSearchService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PhotoInsightTools(
        AppDbContext dbContext,
        IMcpSearchService mcpSearchService,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mcpSearchService = mcpSearchService;
        _httpContextAccessor = httpContextAccessor;
    }

    [McpServerTool(Name = "search_gallery_photos", Title = "使用自然语言检索当前用户的照片库")]
    public async Task<McpSearchResponse> SearchGalleryPhotosAsync(
        [Description("自然语言查询语句，将匹配描述、地点、文件名与标签。")]
        string query,
        [Description("返回的最大结果数量（1-20）。")]
        int limit = 6,
        [Description("可选的起始时间（ISO8601）。")]
        DateTime? from = null,
        [Description("可选的结束时间（ISO8601）。")]
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("查询内容不能为空。", nameof(query));
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ArgumentException("开始时间必须早于结束时间。");
        }

        var userId = EnsureCurrentUserId();
        var request = new McpSearchRequest
        {
            Query = query,
            Limit = Math.Clamp(limit, 1, 20),
            From = from,
            To = to
        };

        return await _mcpSearchService.SearchAsync(request, userId, cancellationToken);
    }

    [McpServerTool(Name = "get_photo_details", Title = "根据照片 ID 返回详细的元数据与标签")]
    public async Task<PhotoDetail?> GetPhotoDetailsAsync(
        [Description("要查询的照片 ID。")]
        int photoId,
        CancellationToken cancellationToken = default)
    {
        var userId = EnsureCurrentUserId();

        var photo = await _dbContext.Photos
            .AsNoTracking()
            .Include(p => p.PhotoTags)
            .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId, cancellationToken);

        return photo == null ? null : MapPhoto(photo);
    }

    private int EnsureCurrentUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            throw new InvalidOperationException("未登录，无法调用照片工具。");
        }

        return userId.Value;
    }

    private static PhotoDetail MapPhoto(Photo photo)
    {
        var tags = photo.PhotoTags
            .Where(pt => pt.Tag != null)
            .Select(pt => new TagDetail(pt.Tag!.Name, pt.Tag.Type))
            .OrderBy(t => t.Type)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PhotoDetail(
            photo.Id,
            photo.FilePath,
            photo.ThumbnailPath,
            photo.Width,
            photo.Height,
            photo.CreatedAt,
            photo.TakenAt,
            photo.Location,
            photo.Description,
            tags);
    }

    public record PhotoDetail(
        int PhotoId,
        string FilePath,
        string ThumbnailPath,
        int Width,
        int Height,
        DateTime CreatedAt,
        DateTime? TakenAt,
        string? Location,
        string? Description,
        IReadOnlyList<TagDetail> Tags);

    public record TagDetail(string Name, TagType Type);
}
