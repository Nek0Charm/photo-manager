using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Directory = System.IO.Directory;
using Path = System.IO.Path;

namespace Backend.Services
{
    public interface IImageService
    {
        Task<PhotoProcessingResult> SavePhotoAsync(IFormFile file, CancellationToken cancellationToken = default);
    }

    public record PhotoProcessingResult(
        string FilePath,
        string ThumbnailPath,
        int Width,
        int Height,
        DateTime? TakenAt,
        string? Location,
        IReadOnlyCollection<string> ExifTags);

    public class ImageService : IImageService
    {
        private readonly string _webRoot;
        private readonly ILogger<ImageService> _logger;

        public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
        {
            if (string.IsNullOrWhiteSpace(environment.WebRootPath))
            {
                var fallbackRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
                Directory.CreateDirectory(fallbackRoot);
                environment.WebRootPath = fallbackRoot;
            }

            _webRoot = environment.WebRootPath!;
            Directory.CreateDirectory(_webRoot);
            _logger = logger;
        }

        public async Task<PhotoProcessingResult> SavePhotoAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("上传文件不能为空", nameof(file));
            }

            var uploadsRoot = Path.Combine(_webRoot, "uploads");
            var originalDir = Path.Combine(uploadsRoot, "original");
            var thumbDir = Path.Combine(uploadsRoot, "thumbs");
            Directory.CreateDirectory(originalDir);
            Directory.CreateDirectory(thumbDir);

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            var safeExtension = extension.ToLowerInvariant();
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{safeExtension}";
            var thumbName = $"{Path.GetFileNameWithoutExtension(fileName)}.thumb.jpg";

            var absoluteOriginalPath = Path.Combine(originalDir, fileName);
            await using (var stream = new FileStream(absoluteOriginalPath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var absoluteThumbPath = Path.Combine(thumbDir, thumbName);

            using var image = await Image.LoadAsync(absoluteOriginalPath, cancellationToken);
            var width = image.Width;
            var height = image.Height;

            var resizeOptions = new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(512, 512)
            };

            using (var thumbnail = image.Clone(ctx => ctx.Resize(resizeOptions)))
            {
                await thumbnail.SaveAsJpegAsync(absoluteThumbPath, new JpegEncoder { Quality = 80 }, cancellationToken);
            }

            DateTime? takenAt = null;
            string? location = null;
            var exifTags = new List<string>();

            try
            {
                var directories = ImageMetadataReader.ReadMetadata(absoluteOriginalPath);
                var exif = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                takenAt = exif?.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal) ??
                          exif?.GetDateTime(ExifDirectoryBase.TagDateTimeDigitized);

                if (takenAt != null)
                {
                    exifTags.Add(takenAt.Value.ToString("yyyy"));
                    exifTags.Add(takenAt.Value.ToString("yyyy-MM"));
                }

                var gps = directories.OfType<GpsDirectory>().FirstOrDefault();
                var geo = gps?.GetGeoLocation();
                if (geo != null && !geo.IsZero)
                {
                    location = $"{geo.Latitude:F4},{geo.Longitude:F4}";
                    exifTags.Add(location);
                }

                var model = directories.OfType<ExifIfd0Directory>().FirstOrDefault()?.GetDescription(ExifDirectoryBase.TagModel);
                if (!string.IsNullOrWhiteSpace(model))
                {
                    exifTags.Add(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取图片 EXIF 数据失败");
            }

            return new PhotoProcessingResult(
                FilePath: $"/uploads/original/{fileName}",
                ThumbnailPath: $"/uploads/thumbs/{thumbName}",
                Width: width,
                Height: height,
                TakenAt: takenAt,
                Location: location,
                ExifTags: exifTags);
        }
    }
}
