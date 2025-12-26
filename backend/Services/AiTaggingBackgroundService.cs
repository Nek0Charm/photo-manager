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
            var tags = await _tagGenerator.GenerateTagsAsync(job.AbsoluteFilePath, cancellationToken);
            if (tags.Count == 0)
            {
                _logger.LogDebug("No AI tags generated for photo {PhotoId}", job.PhotoId);
                return;
            }

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

            RemoveExistingAiTags(photo, db);

            foreach (var tagName in tags)
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

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AI tags applied to photo {PhotoId}: {Tags}", job.PhotoId, string.Join(", ", tags));
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
}
