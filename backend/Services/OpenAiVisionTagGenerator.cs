using System;
using System.Collections.Generic;
using System.ClientModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
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

    public int SuggestionLimit { get; set; } = 5;

    public IReadOnlyList<string> Vocabulary { get; set; } = Array.Empty<string>();
}

public sealed class AiTaggingResult
{
    public static readonly AiTaggingResult Empty = new(Array.Empty<string>(), Array.Empty<string>());

    public AiTaggingResult(IReadOnlyList<string> selected, IReadOnlyList<string> suggested)
    {
        Selected = selected;
        Suggested = suggested;
    }

    public IReadOnlyList<string> Selected { get; }

    public IReadOnlyList<string> Suggested { get; }
}

public interface IAiVisionTagGenerator
{
    Task<AiTaggingResult> GenerateTagsAsync(string absoluteFilePath, AiTaggingOptions options, CancellationToken cancellationToken);
}

public sealed class OpenAiVisionTagGenerator : IAiVisionTagGenerator
{
    private readonly ILogger<OpenAiVisionTagGenerator> _logger;

    public OpenAiVisionTagGenerator(ILogger<OpenAiVisionTagGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<AiTaggingResult> GenerateTagsAsync(string absoluteFilePath, AiTaggingOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _logger.LogDebug("AI tagging skipped because API key is missing.");
            return AiTaggingResult.Empty;
        }

        if (!File.Exists(absoluteFilePath))
        {
            _logger.LogWarning("AI tagging skipped because file {Path} cannot be found.", absoluteFilePath);
            return AiTaggingResult.Empty;
        }

        var normalizedOptions = NormalizeOptions(options);
        var client = CreateClient(normalizedOptions);

        await using var stream = File.OpenRead(absoluteFilePath);
        var imageData = await BinaryData.FromStreamAsync(stream, cancellationToken);
        var mimeType = GetMimeType(Path.GetExtension(absoluteFilePath));

        var vocabularySnippet = BuildVocabularySnippet(normalizedOptions.Vocabulary);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "你是一位资深图片策展人与视觉分类专家。请只输出能够精确描述主要主体、关键场景元素或鲜明情绪的中文名词标签，每个标签不超过 6 个汉字。"
                + "当词表中已有合适标签时，必须优先选用词表；若确无匹配，请在 `suggested` 中提出不超过 "
                + normalizedOptions.SuggestionLimit
                + " 个新标签。始终使用 JSON 对象格式：{\"selected\":[],\"suggested\":[] }，不得附带说明文字。"),
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(
                    $"Inspect this image and respond with JSON as instructed. {vocabularySnippet} 如果图片主体无法辨识，请返回空数组。"),
                ChatMessageContentPart.CreateImagePart(imageData, mimeType))
        };

        try
        {
            var completion = await client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            var text = completion.Value.Content.FirstOrDefault(part => !string.IsNullOrWhiteSpace(part.Text))?.Text;
            return ParseTags(text, normalizedOptions.MaxTags, normalizedOptions.SuggestionLimit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI vision tagging failed for {Path}", absoluteFilePath);
            return AiTaggingResult.Empty;
        }
    }

    private static AiTaggingOptions NormalizeOptions(AiTaggingOptions options)
    {
        var trimmedKey = options.ApiKey?.Trim() ?? string.Empty;
        var normalizedModel = string.IsNullOrWhiteSpace(options.Model) ? "gpt-4o-mini" : options.Model.Trim();
        var normalizedProvider = string.IsNullOrWhiteSpace(options.Provider) ? "OpenAI" : options.Provider.Trim();
        var normalizedEndpoint = string.IsNullOrWhiteSpace(options.Endpoint) ? null : options.Endpoint.Trim();
        var normalizedMaxTags = options.MaxTags <= 0 ? 3 : options.MaxTags;
        var normalizedSuggestionLimit = options.SuggestionLimit <= 0 ? 5 : options.SuggestionLimit;
        var normalizedVocabulary = (options.Vocabulary ?? Array.Empty<string>())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AiTaggingOptions
        {
            Provider = normalizedProvider,
            ApiKey = trimmedKey,
            Model = normalizedModel,
            Endpoint = normalizedEndpoint,
            MaxTags = normalizedMaxTags,
            SuggestionLimit = normalizedSuggestionLimit,
            Vocabulary = normalizedVocabulary
        };
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

    private static string BuildVocabularySnippet(IReadOnlyList<string> vocabulary)
    {
        if (vocabulary.Count == 0)
        {
            return "当前没有额外的词表可以参考。";
        }

        var serialized = JsonSerializer.Serialize(vocabulary);
        return $"请优先从以下词表中选择：{serialized}。";
    }

    private static AiTaggingResult ParseTags(string? raw, int maxTags, int suggestionLimit)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return AiTaggingResult.Empty;
        }

        if (TryParseStructuredJson(raw, maxTags, suggestionLimit, out var selected, out var suggested))
        {
            return new AiTaggingResult(selected, suggested);
        }

        var legacy = ParseLegacyList(raw, maxTags);
        return legacy.Count > 0
            ? new AiTaggingResult(legacy, Array.Empty<string>())
            : AiTaggingResult.Empty;
    }

    private static bool TryParseStructuredJson(string raw, int maxTags, int suggestionLimit, out List<string> selected, out List<string> suggested)
    {
        selected = new List<string>(maxTags);
        suggested = new List<string>(suggestionLimit);
        var candidate = ExtractJsonFragment(raw);

        try
        {
            using var document = JsonDocument.Parse(candidate, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("selected", out var selectedElement))
                {
                    FillListFromJson(selectedElement, selected, maxTags);
                }

                if (root.TryGetProperty("suggested", out var suggestedElement))
                {
                    FillListFromJson(suggestedElement, suggested, suggestionLimit);
                }

                return selected.Count > 0 || suggested.Count > 0;
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                FillListFromJson(root, selected, maxTags);
                return selected.Count > 0;
            }
        }
        catch (JsonException)
        {
            // swallow and fall back to legacy parsing
        }

        selected.Clear();
        suggested.Clear();
        return false;
    }

    private static List<string> ParseLegacyList(string raw, int maxTags)
    {
        var trimmed = ExtractJsonFragment(raw);
        var collected = new List<string>(maxTags);
        var uniques = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

    private static void FillListFromJson(JsonElement element, List<string> buffer, int limit)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var uniques = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in element.EnumerateArray())
        {
            if (buffer.Count >= limit)
            {
                break;
            }

            if (item.ValueKind is not JsonValueKind.String && item.ValueKind is not JsonValueKind.Number)
            {
                continue;
            }

            var normalized = NormalizeTag(item.ToString());
            if (normalized.Length == 0 || !uniques.Add(normalized))
            {
                continue;
            }

            buffer.Add(normalized);
        }
    }

    private static string ExtractJsonFragment(string raw)
    {
        var objectStart = raw.IndexOf('{');
        var objectEnd = raw.LastIndexOf('}');
        if (objectStart >= 0 && objectEnd > objectStart)
        {
            return raw[objectStart..(objectEnd + 1)];
        }

        var arrayStart = raw.IndexOf('[');
        var arrayEnd = raw.LastIndexOf(']');
        if (arrayStart >= 0 && arrayEnd > arrayStart)
        {
            return raw[arrayStart..(arrayEnd + 1)];
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
