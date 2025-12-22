using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Backend.DTOs
{
    public class PhotoUploadRequest
    {
        [Required]
        public IFormFile? File { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 逗号分隔的自定义标签
        /// </summary>
        public string? Tags { get; set; }

        public DateTime? TakenAt { get; set; }

        public string? Location { get; set; }
    }

    public class PhotoEditRequest
    {
        [Required]
        public int PhotoId { get; set; }

        [Required]
        public IFormFile? File { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime? TakenAt { get; set; }

        public string? Location { get; set; }

        /// <summary>
        /// 逗号分隔的自定义标签
        /// </summary>
        public string? Tags { get; set; }

        public bool SaveAsNew { get; set; }
    }

    public class PhotoMetadataUpdateRequest
    {
        [Required]
        public int PhotoId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public IEnumerable<string>? Tags { get; set; }
    }

    public class PhotoDeleteRequest
    {
        [Required]
        public int PhotoId { get; set; }
    }

    public class PhotoListRequest
    {
        private const int MaxPageSize = 60;

        private int _page = 1;
        private int _pageSize = 20;

        [Range(1, int.MaxValue)]
        public int Page
        {
            get => _page;
            set => _page = value <= 0 ? 1 : value;
        }

        [Range(1, MaxPageSize)]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value switch
            {
                <= 0 => 20,
                > MaxPageSize => MaxPageSize,
                _ => value
            };
        }

        public string? Tag { get; set; }

        public string? Keyword { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }
    }

    public class PhotoDetailRequest
    {
        [Required]
        public int Id { get; set; }
    }

    public class PhotoItemDto
    {
        public int Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime? TakenAt { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; }
    }

    public class PhotoListResponse
    {
        public int Total { get; set; }
        public IEnumerable<PhotoItemDto> Items { get; set; } = Enumerable.Empty<PhotoItemDto>();
    }
}
