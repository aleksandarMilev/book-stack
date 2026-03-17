namespace BookStack.Tests.Infrastructure;

using BookStack.Infrastructure.Services.ImageValidator;
using BookStack.Infrastructure.Services.ImageWriter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

public class ImageSecurityHardeningTests : IDisposable
{
    private readonly string _webRootPath = Path.Combine(
        Path.GetTempPath(),
        "bookstack-image-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void ImageValidator_RejectsInvalidImageBytes()
    {
        var validator = new ImageValidator();
        var file = CreateFormFile(
            fileName: "not-image.jpg",
            contentType: "image/jpeg",
            bytes: "not an image"u8.ToArray());

        var result = validator.ValidateImageFile(file);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void ImageValidator_RejectsOversizedFiles()
    {
        var validator = new ImageValidator();

        var bytes = new byte[2 * 1_024 * 1_024 + 1];
        bytes[0] = 0x89;
        bytes[1] = 0x50;
        bytes[2] = 0x4E;
        bytes[3] = 0x47;
        bytes[4] = 0x0D;
        bytes[5] = 0x0A;
        bytes[6] = 0x1A;
        bytes[7] = 0x0A;

        var file = CreateFormFile(
            fileName: "big.png",
            contentType: "image/png",
            bytes: bytes);

        var result = validator.ValidateImageFile(file);

        Assert.False(result.Succeeded);
        Assert.Contains("smaller than", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImageValidator_RejectsMismatchedExtensionAndHeader()
    {
        var validator = new ImageValidator();

        var pngHeaderBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D,
        };

        var file = CreateFormFile(
            fileName: "fake.jpg",
            contentType: "image/jpeg",
            bytes: pngHeaderBytes);

        var result = validator.ValidateImageFile(file);

        Assert.False(result.Succeeded);
        Assert.Contains("does not match", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImageWriter_Delete_SkipsDefaultImagePath()
    {
        Directory.CreateDirectory(Path.Combine(this._webRootPath, "images", "listings"));

        var defaultPath = "/images/listings/default.jpg";
        var defaultPhysicalPath = Path.Combine(
            this._webRootPath,
            "images",
            "listings",
            "default.jpg");

        File.WriteAllText(defaultPhysicalPath, "default");

        var writer = this.CreateImageWriter();

        var deleted = writer.Delete(
            "listings",
            defaultPath,
            defaultPath);

        Assert.False(deleted);
        Assert.True(File.Exists(defaultPhysicalPath));
    }

    [Fact]
    public void ImageWriter_Delete_RejectsPathTraversal()
    {
        Directory.CreateDirectory(Path.Combine(this._webRootPath, "images", "listings"));
        File.WriteAllText(Path.Combine(this._webRootPath, "secret.txt"), "secret");

        var writer = this.CreateImageWriter();

        var deleted = writer.Delete(
            "listings",
            "/images/listings/../../secret.txt",
            "/images/listings/default.jpg");

        Assert.False(deleted);
        Assert.True(File.Exists(Path.Combine(this._webRootPath, "secret.txt")));
    }

    [Fact]
    public void ImageWriter_Delete_OnlyDeletesTargetOldImage()
    {
        var listingRoot = Path.Combine(this._webRootPath, "images", "listings");
        Directory.CreateDirectory(listingRoot);

        var oldPath = Path.Combine(listingRoot, "old.jpg");
        var newPath = Path.Combine(listingRoot, "new.jpg");

        File.WriteAllText(oldPath, "old");
        File.WriteAllText(newPath, "new");

        var writer = this.CreateImageWriter();

        var deleted = writer.Delete(
            "listings",
            "/images/listings/old.jpg",
            "/images/listings/default.jpg");

        Assert.True(deleted);
        Assert.False(File.Exists(oldPath));
        Assert.True(File.Exists(newPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(this._webRootPath))
        {
            Directory.Delete(this._webRootPath, recursive: true);
        }
    }

    private ImageWriter CreateImageWriter()
        => new(
            new ImageValidator(),
            NullLogger<ImageWriter>.Instance,
            new TestWebHostEnvironment
            {
                WebRootPath = this._webRootPath,
                ContentRootPath = this._webRootPath,
            });

    private static IFormFile CreateFormFile(
        string fileName,
        string contentType,
        byte[] bytes)
    {
        var stream = new MemoryStream(bytes);

        return new FormFile(
            stream,
            baseStreamOffset: 0,
            length: bytes.Length,
            name: "image",
            fileName: fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "BookStack.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = "Testing";

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
