using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Backend.Services;

public interface IAiTaggingQueue
{
    ValueTask QueueAsync(int photoId, string? absoluteFilePath, CancellationToken cancellationToken = default);
}

internal sealed record AiTaggingJob(int PhotoId, string AbsoluteFilePath);

public sealed class AiTaggingBackgroundService : BackgroundService, IAiTaggingQueue
{
    private static readonly string[] DefaultVocabulary =
    {
        "人像", "肖像", "街头", "城市夜景", "城市街道", "海滩", "海浪", "港口", "山脉", "雪山",
        "森林", "湖泊", "河流", "瀑布", "草原", "沙漠", "日出", "日落", "逆光", "黄昏",
        "星空", "云海", "暴雨", "彩虹", "室内", "静物", "美食", "甜点", "咖啡", "宠物",
        "猫咪", "狗狗", "花卉", "樱花", "荷花", "建筑", "古建筑", "现代建筑", "天空", "航拍",
        "广角", "微距", "体育", "旅行", "情侣", "婚礼", "儿童", "节日", "烟花", "露营"
    };

    private readonly Channel<AiTaggingJob> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAiVisionTagGenerator _tagGenerator;
    private readonly ILogger<AiTaggingBackgroundService> _logger;

    public AiTaggingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IAiVisionTagGenerator tagGenerator,
        ILogger<AiTaggingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _tagGenerator = tagGenerator;
        _logger = logger;
        _channel = Channel.CreateUnbounded<AiTaggingJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask QueueAsync(int photoId, string? absoluteFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(absoluteFilePath))
        {
            _logger.LogDebug("Skip AI tagging queue for photo {PhotoId} because absolute path is empty", photoId);
            return ValueTask.CompletedTask;
        }

        var job = new AiTaggingJob(photoId, absoluteFilePath);
        if (_channel.Writer.TryWrite(job))
        {
            _logger.LogDebug("Enqueued AI tagging job for photo {PhotoId}", photoId);
            return ValueTask.CompletedTask;
        }

        return _channel.Writer.WriteAsync(job, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(job, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(AiTaggingJob job, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var photo = await db.Photos
                .Include(p => p.PhotoTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == job.PhotoId, cancellationToken);

            if (photo == null)
            {
                _logger.LogWarning("Photo {PhotoId} no longer exists while applying AI tags", job.PhotoId);
                return;
            }

            var aiSettings = await db.UserAiSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == photo.UserId, cancellationToken);

            if (aiSettings == null || string.IsNullOrWhiteSpace(aiSettings.ApiKey))
            {
                _logger.LogDebug("Skipping AI tagging for photo {PhotoId} because user {UserId} has no API key configured", job.PhotoId, photo.UserId);
                return;
            }

            var vocabulary = await BuildVocabularyAsync(db, photo.UserId, cancellationToken);

            var options = new AiTaggingOptions
            {
                Provider = aiSettings.Provider,
                ApiKey = aiSettings.ApiKey,
                Model = aiSettings.Model,
                Endpoint = aiSettings.Endpoint,
                MaxTags = 3,
                SuggestionLimit = 5,
                Vocabulary = vocabulary
            };

            var result = await _tagGenerator.GenerateTagsAsync(job.AbsoluteFilePath, options, cancellationToken);
            if (result.Selected.Count == 0 && result.Suggested.Count == 0)
            {
                _logger.LogDebug("No AI tags generated for photo {PhotoId}", job.PhotoId);
                return;
            }

            if (result.Selected.Count > 0)
            {
                RemoveExistingAiTags(photo, db);

                foreach (var tagName in result.Selected)
                {
                    var normalized = tagName.Trim();
                    if (string.IsNullOrWhiteSpace(normalized))
                    {
                        continue;
                    }

                    var tag = await db.Tags.FirstOrDefaultAsync(
                                  t => t.Name == normalized && t.Type == TagType.Ai,
                                  cancellationToken)
                              ?? new Tag { Name = normalized, Type = TagType.Ai };

                    if (tag.Id == 0)
                    {
                        db.Tags.Add(tag);
                    }

                    photo.PhotoTags.Add(new PhotoTag { Photo = photo, Tag = tag });
                }
            }

            if (result.Suggested.Count > 0)
            {
                await PersistSuggestionsAsync(db, photo, result.Suggested, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "AI tags applied to photo {PhotoId}: {Tags}; pending suggestions: {Suggestions}",
                job.PhotoId,
                string.Join(", ", result.Selected),
                string.Join(", ", result.Suggested));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process AI tagging job for photo {PhotoId}", job.PhotoId);
        }
    }

    private static void RemoveExistingAiTags(Photo photo, AppDbContext db)
    {
        var current = photo.PhotoTags
            .Where(pt => pt.Tag != null && pt.Tag.Type == TagType.Ai)
            .ToList();

        if (current.Count == 0)
        {
            return;
        }

        db.PhotoTags.RemoveRange(current);
        foreach (var relation in current)
        {
            photo.PhotoTags.Remove(relation);
        }
    }

    private static async Task<IReadOnlyList<string>> BuildVocabularyAsync(AppDbContext db, int userId, CancellationToken cancellationToken)
    {
        var personal = await db.PhotoTags
            .Where(pt => pt.Photo != null
                         && pt.Photo.UserId == userId
                         && pt.Tag != null
                         && (pt.Tag.Type == TagType.Manual || pt.Tag.Type == TagType.Exif))
            .GroupBy(pt => pt.Tag!.Name)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .Take(50)
            .ToListAsync(cancellationToken);

        return DefaultVocabulary
            .Concat(personal)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task PersistSuggestionsAsync(AppDbContext db, Photo photo, IReadOnlyList<string> suggestions, CancellationToken cancellationToken)
    {
        foreach (var suggestion in suggestions)
        {
            var normalized = suggestion.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            var exists = await db.AiTagSuggestions.AnyAsync(
                s => s.UserId == photo.UserId && s.Name == normalized,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            db.AiTagSuggestions.Add(new AiTagSuggestion
            {
                UserId = photo.UserId,
                PhotoId = photo.Id,
                Name = normalized,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
