namespace BookStack.Tests.TestInfrastructure;

using BookStack.Infrastructure.Services.ImageWriter;
using BookStack.Infrastructure.Services.ImageWriter.Models;

internal sealed class FakeImageWriter : IImageWriter
{
    public Task Write(
        string resourceName,
        IImageDdModel dbModel,
        IImageServiceModel serviceModel,
        string? defaultImagePath = null,
        CancellationToken cancellationToken = default)
    {
        if (serviceModel.Image is not null)
        {
            dbModel.ImagePath = $"/images/{resourceName}/fake-upload.jpg";
            return Task.CompletedTask;
        }

        if (!string.IsNullOrWhiteSpace(defaultImagePath))
        {
            dbModel.ImagePath = defaultImagePath;
        }

        return Task.CompletedTask;
    }

    public bool Delete(
        string resourceName,
        string? imagePath,
        string? defaultImagePath = null)
        => true;
}
