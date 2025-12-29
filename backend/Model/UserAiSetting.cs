using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class UserAiSetting
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = "OpenAI";

        [Required]
        [MaxLength(200)]
        public string Model { get; set; } = "gpt-4o-mini";

        [MaxLength(500)]
        public string? Endpoint { get; set; }

        [Required]
        [MaxLength(512)]
        public string ApiKey { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
