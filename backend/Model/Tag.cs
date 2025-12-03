using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public enum TagType
    {
        Manual = 0,
        Ai = 1,
        Exif = 2
    }

    public class Tag
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public TagType Type { get; set; }

        public ICollection<PhotoTag> PhotoTags { get; set; } = new List<PhotoTag>();
    }
}
