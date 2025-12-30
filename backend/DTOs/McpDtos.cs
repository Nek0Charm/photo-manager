using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Backend.DTOs
{
    public class McpSearchRequest
    {
        private const int MaxLimit = 20;
        private int _limit = 6;

        [Required]
        [MaxLength(200)]
        public string Query { get; set; } = string.Empty;

        [Range(1, MaxLimit)]
        public int Limit
        {
            get => _limit;
            set => _limit = value switch
            {
                <= 0 => 6,
                > MaxLimit => MaxLimit,
                _ => value
            };
        }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class McpContentBlock
    {
        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "text";

        [Required]
        [MaxLength(1000)]
        public string Text { get; set; } = string.Empty;
    }

    public class McpSearchResult
    {
        public string DocumentId { get; set; } = string.Empty;
        public double Score { get; set; }
        public IEnumerable<McpContentBlock> Content { get; set; } = Array.Empty<McpContentBlock>();
        public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();
    }

    public class McpSearchResponse
    {
        public int Total { get; set; }
        public IEnumerable<McpSearchResult> Results { get; set; } = Enumerable.Empty<McpSearchResult>();
    }
}
