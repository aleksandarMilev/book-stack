namespace BookStack.Infrastructure.Services.ImageValidator;

using System.Collections.Frozen;
using System.Text;
using Result;

public class ImageValidator : IImageValidator
{
    private const int MaxImageSizeBytes = 2 * 1_024 * 1_024;

    private enum ImageFormat
    {
        Unknown = 0,
        Jpeg = 1,
        Png = 2,
        Webp = 3,
        Avif = 4,
    }

    private static readonly FrozenSet<string> AllowedExtensions =
        new[] { ".jpg", ".jpeg", ".png", ".webp", ".avif" }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> AllowedContentTypes =
        new[] { "image/jpeg", "image/png", "image/webp", "image/avif" }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public Result ValidateImageFile(IFormFile image)
    {
        if (image.Length == 0)
        {
            return "Image file is empty.";
        }

        if (image.Length > MaxImageSizeBytes)
        {
            return $"Image must be smaller than {MaxImageSizeBytes / 1_024 / 1_024} MB.";
        }

        var extension = Path.GetExtension(image.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return $"Invalid image extension. Allowed: {string.Join(", ", AllowedExtensions)}.";
        }

        if (!AllowedContentTypes.Contains(image.ContentType))
        {
            return $"Invalid image content type. Allowed: {string.Join(", ", AllowedContentTypes)}.";
        }

        var detectedFormat = DetectImageFormat(image);
        if (detectedFormat == ImageFormat.Unknown)
        {
            return "File content does not match an allowed image format.";
        }

        if (!IsMatchingExtension(detectedFormat, extension))
        {
            return "File extension does not match the uploaded image format.";
        }

        if (!IsMatchingContentType(detectedFormat, image.ContentType))
        {
            return "Image content type does not match the uploaded image format.";
        }

        return true;
    }

    private static ImageFormat DetectImageFormat(IFormFile file)
    {
        Span<byte> header = stackalloc byte[16];

        using var stream = file.OpenReadStream();
        var read = stream.Read(header);

        const int MinHeaderBytes = 12;
        if (read < MinHeaderBytes)
        {
            return ImageFormat.Unknown;
        }

        var looksLikeJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        if (looksLikeJpeg)
        {
            return ImageFormat.Jpeg;
        }

        const int PngSignatureLength = 8;
        var hasPngHeader = read >= PngSignatureLength;

        var looksLikePng = hasPngHeader &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;
        if (looksLikePng)
        {
            return ImageFormat.Png;
        }

        var hasRiffHeader = read >= MinHeaderBytes;
        var looksLikeWebp = hasRiffHeader &&
            header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
            header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';
        if (looksLikeWebp)
        {
            return ImageFormat.Webp;
        }

        var looksLikeIsoBmff = hasRiffHeader &&
            header[4] == (byte)'f' && header[5] == (byte)'t' && header[6] == (byte)'y' && header[7] == (byte)'p';

        if (looksLikeIsoBmff)
        {
            var brand = Encoding.ASCII.GetString(header.Slice(8, 4));
            if (brand is "avif" or "avis")
            {
                return ImageFormat.Avif;
            }
        }

        return ImageFormat.Unknown;
    }

    private static bool IsMatchingExtension(
        ImageFormat format,
        string extension)
        => format switch
        {
            ImageFormat.Jpeg => extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase),
            ImageFormat.Png => extension.Equals(".png", StringComparison.OrdinalIgnoreCase),
            ImageFormat.Webp => extension.Equals(".webp", StringComparison.OrdinalIgnoreCase),
            ImageFormat.Avif => extension.Equals(".avif", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };

    private static bool IsMatchingContentType(
        ImageFormat format,
        string contentType)
    {
        var normalizedContentType = contentType.Trim();

        return format switch
        {
            ImageFormat.Jpeg => normalizedContentType.Equals(
                "image/jpeg",
                StringComparison.OrdinalIgnoreCase),
            ImageFormat.Png => normalizedContentType.Equals(
                "image/png",
                StringComparison.OrdinalIgnoreCase),
            ImageFormat.Webp => normalizedContentType.Equals(
                "image/webp",
                StringComparison.OrdinalIgnoreCase),
            ImageFormat.Avif => normalizedContentType.Equals(
                "image/avif",
                StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }
}
