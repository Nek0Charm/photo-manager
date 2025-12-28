using System;
using System.Collections.Generic;
using System.ClientModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Backend.Services;

public sealed class AiTaggingOptions
{
    public string Provider { get; set; } = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-4o-mini";

    public string? Endpoint { get; set; }

    public int MaxTags { get; set; } = 3;
}

public interface IAiVisionTagGenerator
{
    Task<IReadOnlyList<string>> GenerateTagsAsync(string absoluteFilePath, CancellationToken cancellationToken);
}

public sealed class OpenAiVisionTagGenerator : IAiVisionTagGenerator
{
    private readonly IOptionsMonitor<AiTaggingOptions> _optionsMonitor;
    private readonly ILogger<OpenAiVisionTagGenerator> _logger;

    public OpenAiVisionTagGenerator(
        IOptionsMonitor<AiTaggingOptions> optionsMonitor,
        ILogger<OpenAiVisionTagGenerator> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GenerateTagsAsync(string absoluteFilePath, CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _logger.LogDebug("AI tagging skipped because API key is missing.");
            return Array.Empty<string>();
        }

        if (!File.Exists(absoluteFilePath))
        {
            _logger.LogWarning("AI tagging skipped because file {Path} cannot be found.", absoluteFilePath);
            return Array.Empty<string>();
        }

        var client = CreateClient(options);

        await using var stream = File.OpenRead(absoluteFilePath);
        var imageData = await BinaryData.FromStreamAsync(stream, cancellationToken);
        var mimeType = GetMimeType(Path.GetExtension(absoluteFilePath));

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "你是一位资深图片策展人与视觉分类专家。请只输出能够精确描述主要主体、关键场景元素或鲜明情绪的中文名词标签，每个标签不超过 6 个汉字，避免如‘自然’‘风景’等泛泛词汇。"
                +"请按照重要性输出 1 到 3 个唯一标签，使用 JSON 数组格式（例如 [\"日落\",\"雪山\"]），不得包含解释、序号或额外文本。"),
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(
                    "Inspect this image and return only that JSON array of concise tags; reply [] if no meaningful subject is visible."),
                ChatMessageContentPart.CreateImagePart(imageData, mimeType))
        };

        try
        {
            var completion = await client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            var text = completion.Value.Content.FirstOrDefault(part => !string.IsNullOrWhiteSpace(part.Text))?.Text;
            return ParseTags(text, options.MaxTags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI vision tagging failed for {Path}", absoluteFilePath);
            return Array.Empty<string>();
        }
    }

    private static ChatClient CreateClient(AiTaggingOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return new ChatClient(
                model: options.Model,
                credential: new ApiKeyCredential(options.ApiKey),
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(options.Endpoint, UriKind.Absolute)
                });
        }

        return new ChatClient(options.Model, options.ApiKey);
    }

    private static IReadOnlyList<string> ParseTags(string? raw, int maxTags)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        var collected = new List<string>(maxTags);
        var uniques = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (TryParseJson(raw, maxTags, collected, uniques))
        {
            return collected;
        }

        var trimmed = ExtractArray(raw);
        foreach (var part in trimmed.Split(new[] { ',', '\n', ';', '|', '，', '、' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var normalized = NormalizeTag(part);
            if (normalized.Length == 0 || !uniques.Add(normalized))
            {
                continue;
            }

            collected.Add(normalized);
            if (collected.Count >= maxTags)
            {
                break;
            }
        }

        return collected;
    }

    private static bool TryParseJson(string raw, int maxTags, List<string> buffer, HashSet<string> uniques)
    {
        var candidate = ExtractArray(raw);
        try
        {
            var parsed = JsonSerializer.Deserialize<string[]>(candidate, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (parsed == null)
            {
                return false;
            }

            foreach (var entry in parsed)
            {
                var normalized = NormalizeTag(entry);
                if (normalized.Length == 0 || !uniques.Add(normalized))
                {
                    continue;
                }

                buffer.Add(normalized);
                if (buffer.Count >= maxTags)
                {
                    break;
                }
            }

            return buffer.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ExtractArray(string raw)
    {
        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        if (start >= 0 && end > start)
        {
            return raw[start..(end + 1)];
        }

        return raw;
    }

    private static string NormalizeTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Empty;
        }

        var trimmed = tag.Trim().Trim('\"', '\'', '#');
        trimmed = trimmed.Replace('\r', ' ').Replace('\n', ' ');
        trimmed = string.Join('-', trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return trimmed.ToLowerInvariant();
    }

    private static string GetMimeType(string? extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            _ => "image/jpeg"
        };
    }
}
