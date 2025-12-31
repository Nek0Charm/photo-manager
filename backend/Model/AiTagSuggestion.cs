using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class AiTagSuggestion
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int PhotoId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsAdopted { get; set; }

    public DateTime? AdoptedAt { get; set; }

    public Photo? Photo { get; set; }
}
