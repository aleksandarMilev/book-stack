namespace BookStack.Infrastructure.Services.ImageWriter;

using ImageValidator;
using Models;

public class ImageWriter(
    IImageValidator imageValidator,
    ILogger<ImageWriter> logger,
    IWebHostEnvironment env) : IImageWriter
{
    private const string ImagesPathPrefix = "images";

    public async Task Write(
        string resourceName,
        IImageDbModel dbModel,
        IImageServiceModel serviceModel,
        string? defaultImagePath = null,
        CancellationToken cancelationToken = default)
    {
        if (serviceModel.Image is not null)
        {
            var validationResult = imageValidator
                .ValidateImageFile(serviceModel.Image);

            if (!validationResult.Succeeded)
            {
                logger.LogWarning(
                    "Invalid image upload for {resourceName}. Error: {Error}.",
                    resourceName,
                    validationResult.ErrorMessage);

                throw new InvalidOperationException(validationResult.ErrorMessage);
            }

            await this.SaveImageFile(
                resourceName,
                dbModel,
                serviceModel,
                cancelationToken);
        }
        else 
        {
            if (defaultImagePath is not null)
            {
                dbModel.ImagePath = defaultImagePath;
            }
        }
    }

    public bool Delete(
        string resourceName,
        string? imagePath,
        string? defaultImagePath = null)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            logger.LogInformation(
                "Delete skipped for {Resource}: empty imagePath.",
                resourceName);

            return false;
        }

        var isDefaultImagePath = 
            !string.IsNullOrWhiteSpace(defaultImagePath) &&
            string.Equals(
                imagePath,
                defaultImagePath,
                StringComparison.OrdinalIgnoreCase);

        if (isDefaultImagePath)
        {
            logger.LogInformation(
                "Delete skipped for {Resource}: imagePath is default. Path={Path}",
                resourceName,
                imagePath);

            return false;
        }

        var isRemoteImageUrl =
            imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (isRemoteImageUrl)
        {
            logger.LogInformation(
                "Delete skipped for {resourceName}: remote URL. Path={Path}",
                resourceName,
                imagePath);

            return false;
        }

        var normalizedRelativePath = NormalizeRelativeImagePath(imagePath);
        if (string.IsNullOrWhiteSpace(normalizedRelativePath))
        {
            logger.LogWarning(
                "Delete skipped for {Resource}: image path could not be normalized safely. Path={Path}",
                resourceName,
                imagePath);

            return false;
        }

        var expectedPrefix = $"{ImagesPathPrefix}/{resourceName}/";
        if (!normalizedRelativePath.StartsWith(
                expectedPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Delete skipped for {Resource}: path is outside allowed resource folder. Path={Path}, ExpectedPrefix={ExpectedPrefix}",
                resourceName,
                normalizedRelativePath,
                expectedPrefix);

            return false;
        }

        var fileName = Path.GetFileName(normalizedRelativePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            logger.LogWarning(
                "Delete skipped for {Resource}: resolved filename is empty. Path={Path}",
                resourceName,
                imagePath);

            return false;
        }

        var resourceRoot = Path.GetFullPath(
            Path.Combine(
                env.WebRootPath,
                ImagesPathPrefix,
                resourceName));

        var resolvedPath = Path.GetFullPath(
            Path.Combine(resourceRoot, fileName));

        var normalizedResourceRoot = resourceRoot
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        var isInsideResourceRoot = resolvedPath.StartsWith(
            normalizedResourceRoot,
            StringComparison.OrdinalIgnoreCase);

        if (!isInsideResourceRoot)
        {
            logger.LogWarning(
                "Delete skipped for {Resource}: resolved path escapes resource root. Resolved={Resolved}, ResourceRoot={ResourceRoot}",
                resourceName,
                resolvedPath,
                resourceRoot);

            return false;
        }

        var exists = File.Exists(resolvedPath);

        logger.LogInformation(
            "Delete attempt. Resource={Resource}, ImagePath={ImagePath}, WebRoot={WebRoot}, Resolved={Resolved}, Exists={Exists}",
            resourceName,
            imagePath,
            env.WebRootPath,
            resolvedPath,
            exists);

        if (!exists)
        {
            return false;
        }

        try
        {
            File.Delete(resolvedPath);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to delete image file. Resource={Resource}, Resolved={Resolved}",
                resourceName,
                resolvedPath);

            return false;
        }

        logger.LogInformation(
            "Deleted image file. Resource={Resource}, Resolved={Resolved}",
            resourceName,
            resolvedPath);

        return true;
    }

    private async Task SaveImageFile(
        string resourceName,
        IImageDbModel dbModel,
        IImageServiceModel serviceModel,
        CancellationToken cancellationToken = default)
    {
        if (serviceModel.Image is null)
        {
            throw new ArgumentException(
                "Service model's Image property is not set.",
                nameof(serviceModel));
        }

        var extension = Path
            .GetExtension(serviceModel.Image.FileName)
            .ToLowerInvariant();

        var fileName = $"{Guid.NewGuid()}{extension}";
        var uploadsRoot = Path.Combine(
            env.WebRootPath,
            ImagesPathPrefix,
            resourceName);

        Directory.CreateDirectory(uploadsRoot);

        var filePath = Path.Combine(uploadsRoot, fileName);

        try
        {
            await using var stream = new FileStream(filePath, FileMode.Create);
            await serviceModel.Image.CopyToAsync(stream, cancellationToken);

            dbModel.ImagePath = $"/{ImagesPathPrefix}/{resourceName}/{fileName}";
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error saving {resourceName} image to path {Path}",
                resourceName,
                filePath);
        }
    }

    private static string? NormalizeRelativeImagePath(string imagePath)
    {
        var trimmedPath = imagePath.Trim();
        var withoutLeadingSlashes = trimmedPath.TrimStart('/', '\\');
        if (withoutLeadingSlashes.Length == 0)
        {
            return null;
        }

        var normalizedPath = withoutLeadingSlashes.Replace('\\', '/');

        // Reject directory traversal segments before path resolution.
        if (normalizedPath.Contains("../", StringComparison.Ordinal) ||
            normalizedPath.Contains("..\\", StringComparison.Ordinal))
        {
            return null;
        }

        return normalizedPath;
    }
}
