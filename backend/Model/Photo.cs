using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Photo
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [MaxLength(255)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string ThumbnailPath { get; set; } = string.Empty;

        public int Width { get; set; }

        public int Height { get; set; }

        public DateTime? TakenAt { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PhotoTag> PhotoTags { get; set; } = new List<PhotoTag>();
    }
}
