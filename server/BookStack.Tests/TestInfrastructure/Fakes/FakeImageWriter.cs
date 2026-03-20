namespace BookStack.Tests.TestInfrastructure.Fakes;

using BookStack.Infrastructure.Services.ImageWriter;
using BookStack.Infrastructure.Services.ImageWriter.Models;

internal sealed class FakeImageWriter : IImageWriter
{
    public List<WriteCallRecord> WriteCalls { get; } = [];

    public List<DeleteCallRecord> DeleteCalls { get; } = [];

    public Queue<string> NextUploadedImagePaths { get; } = [];

    public bool DeleteResult { get; set; } = true;

    public Task Write(
        string resourceName,
        IImageDbModel dbModel,
        IImageServiceModel serviceModel,
        string? defaultImagePath = null,
        CancellationToken cancellationToken = default)
    {
        this.WriteCalls.Add(new(
            resourceName,
            serviceModel.Image is not null,
            defaultImagePath));

        if (serviceModel.Image is not null)
        {
            var uploadedImagePath = this.NextUploadedImagePaths.Count > 0
                ? this.NextUploadedImagePaths.Dequeue()
                : $"/images/{resourceName}/fake-upload.jpg";

            dbModel.ImagePath = uploadedImagePath;
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
    {
        this.DeleteCalls.Add(new(
            resourceName,
            imagePath,
            defaultImagePath));

        return this.DeleteResult;
    }

    public sealed record WriteCallRecord(
        string ResourceName,
        bool HasImage,
        string? DefaultImagePath);

    public sealed record DeleteCallRecord(
        string ResourceName,
        string? ImagePath,
        string? DefaultImagePath);
}
