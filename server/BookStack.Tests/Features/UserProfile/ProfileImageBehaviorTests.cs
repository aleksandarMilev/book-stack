namespace BookStack.Tests.Features.UserProfile;

using BookStack.Data;
using BookStack.Features.UserProfile.Data.Models;
using BookStack.Features.UserProfile.Service;
using BookStack.Features.UserProfile.Service.Models;
using BookStack.Features.UserProfile.Shared;
using BookStack.Infrastructure.Services.ImageWriter;
using BookStack.Infrastructure.Services.ImageWriter.Models;
using BookStack.Infrastructure.Services.StringSanitizer;
using BookStack.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using static BookStack.Features.UserProfile.Shared.Constants.Paths;

public class ProfileImageBehaviorTests
{
    [Fact]
    public async Task Create_ProfileWithoutImage_UsesDefaultImagePath()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "registration-user",
            Username = "registration-user",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 11, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        await EnsureUser(data, currentUserService.UserId!);
        var imageWriter = new TrackingImageWriter();
        var service = CreateProfileService(data, currentUserService, imageWriter);

        var result = await service.Create(
            new CreateProfileServiceModel
            {
                FirstName = "Alice",
                LastName = "Reader",
                Image = null,
                RemoveImage = false,
            },
            currentUserService.UserId!,
            CancellationToken.None);

        var persistedProfile = await data
            .Profiles
            .AsNoTracking()
            .SingleAsync(p => p.UserId == currentUserService.UserId, CancellationToken.None);

        Assert.Equal(DefaultImagePath, result.ImagePath);
        Assert.Equal(DefaultImagePath, persistedProfile.ImagePath);
    }

    [Fact]
    public async Task Edit_WithoutNewImage_PreservesExistingImagePath()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "profile-user-preserve",
            Username = "profile-user-preserve",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 12, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        const string ExistingImagePath = "/images/profiles/existing-custom.jpg";

        await EnsureProfile(
            data,
            currentUserService.UserId!,
            ExistingImagePath);

        var imageWriter = new TrackingImageWriter();
        var service = CreateProfileService(data, currentUserService, imageWriter);

        var result = await service.Edit(
            new CreateProfileServiceModel
            {
                FirstName = "Alice",
                LastName = "Updated",
                Image = null,
                RemoveImage = false,
            },
            CancellationToken.None);

        var persistedProfile = await data
            .Profiles
            .AsNoTracking()
            .SingleAsync(p => p.UserId == currentUserService.UserId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(ExistingImagePath, persistedProfile.ImagePath);
        Assert.Empty(imageWriter.DeleteCalls);
    }

    [Fact]
    public async Task Edit_RemoveImage_ResetsToDefaultAndDeletesOldCustomImage()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "profile-user-remove",
            Username = "profile-user-remove",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 13, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        const string ExistingImagePath = "/images/profiles/existing-to-remove.jpg";

        await EnsureProfile(
            data,
            currentUserService.UserId!,
            ExistingImagePath);

        var imageWriter = new TrackingImageWriter();
        var service = CreateProfileService(data, currentUserService, imageWriter);

        var result = await service.Edit(
            new CreateProfileServiceModel
            {
                FirstName = "Alice",
                LastName = "Updated",
                Image = null,
                RemoveImage = true,
            },
            CancellationToken.None);

        var persistedProfile = await data
            .Profiles
            .AsNoTracking()
            .SingleAsync(p => p.UserId == currentUserService.UserId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(DefaultImagePath, persistedProfile.ImagePath);
        Assert.Single(imageWriter.DeleteCalls);
        Assert.Equal(ExistingImagePath, imageWriter.DeleteCalls[0].ImagePath);
        Assert.Equal(DefaultImagePath, imageWriter.DeleteCalls[0].DefaultImagePath);
    }

    [Fact]
    public async Task Edit_WithCustomUploadedImage_ReplacesOldImageAndDeletesPreviousCustomImage()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "profile-user-custom-upload",
            Username = "profile-user-custom-upload",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 14, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        const string ExistingImagePath = "/images/profiles/previous-custom.jpg";

        await EnsureProfile(
            data,
            currentUserService.UserId!,
            ExistingImagePath);

        var imageWriter = new TrackingImageWriter();
        var service = CreateProfileService(data, currentUserService, imageWriter);

        var result = await service.Edit(
            new CreateProfileServiceModel
            {
                FirstName = "Alice",
                LastName = "Updated",
                Image = CreateFormFile("avatar.png", "image/png", "custom-avatar"),
                RemoveImage = false,
            },
            CancellationToken.None);

        var persistedProfile = await data
            .Profiles
            .AsNoTracking()
            .SingleAsync(p => p.UserId == currentUserService.UserId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(TrackingImageWriter.UploadedImagePath, persistedProfile.ImagePath);
        Assert.Single(imageWriter.DeleteCalls);
        Assert.Equal(ExistingImagePath, imageWriter.DeleteCalls[0].ImagePath);
    }

    [Fact]
    public async Task Edit_WhenOldImageIsDefault_DoesNotAttemptToDeleteSharedDefaultAsset()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "profile-user-shared-default",
            Username = "profile-user-shared-default",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 15, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        await EnsureProfile(
            data,
            currentUserService.UserId!,
            DefaultImagePath);

        var imageWriter = new TrackingImageWriter();
        var service = CreateProfileService(data, currentUserService, imageWriter);

        var result = await service.Edit(
            new CreateProfileServiceModel
            {
                FirstName = "Alice",
                LastName = "Updated",
                Image = CreateFormFile("avatar.png", "image/png", "custom-avatar"),
                RemoveImage = false,
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(imageWriter.DeleteCalls);
    }

    private static ProfileService CreateProfileService(
        BookStackDbContext data,
        TestCurrentUserService currentUserService,
        IImageWriter imageWriter)
        => new(
            data,
            userManager: null!,
            currentUserService,
            imageWriter,
            new StringSanitizerService(),
            NullLogger<ProfileService>.Instance);

    private static async Task EnsureUser(
        BookStackDbContext data,
        string userId)
    {
        var userExists = await data.Users.AnyAsync(u => u.Id == userId, CancellationToken.None);
        if (userExists)
        {
            return;
        }

        data.Users.Add(MarketplaceTestData.CreateUser(
            userId,
            $"{userId}@example.com"));

        await data.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task EnsureProfile(
        BookStackDbContext data,
        string userId,
        string imagePath)
    {
        await EnsureUser(data, userId);

        var existingProfile = await data
            .Profiles
            .SingleOrDefaultAsync(p => p.UserId == userId, CancellationToken.None);

        if (existingProfile is null)
        {
            data.Profiles.Add(new UserProfileDbModel
            {
                UserId = userId,
                FirstName = "Alice",
                LastName = "Reader",
                ImagePath = imagePath,
            });
        }
        else
        {
            existingProfile.ImagePath = imagePath;
        }

        await data.SaveChangesAsync(CancellationToken.None);
    }

    private static IFormFile CreateFormFile(
        string fileName,
        string contentType,
        string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "image", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class TrackingImageWriter : IImageWriter
    {
        public const string UploadedImagePath = "/images/profiles/custom-upload.jpg";

        public List<DeleteCall> DeleteCalls { get; } = [];

        public Task Write(
            string resourceName,
            IImageDdModel dbModel,
            IImageServiceModel serviceModel,
            string? defaultImagePath = null,
            CancellationToken cancellationToken = default)
        {
            if (serviceModel.Image is not null)
            {
                dbModel.ImagePath = UploadedImagePath;
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
            this.DeleteCalls.Add(new DeleteCall(resourceName, imagePath, defaultImagePath));
            return true;
        }
    }

    private sealed record DeleteCall(
        string ResourceName,
        string? ImagePath,
        string? DefaultImagePath);
}
