using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class AiSettingsResponse
    {
        public string Provider { get; set; } = "OpenAI";

        public string Model { get; set; } = "gpt-4o-mini";

        public string? Endpoint { get; set; }

        public bool HasApiKey { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public class AiSettingsUpsertRequest
    {
        [Required]
        [MinLength(3, ErrorMessage = "模型名称至少 3 个字符")]
        public string Model { get; set; } = "gpt-4o-mini";

        [MaxLength(500)]
        public string? Endpoint { get; set; }

        [MaxLength(512)]
        public string? ApiKey { get; set; }

        public bool UpdateApiKey { get; set; } = true;
    }
}
