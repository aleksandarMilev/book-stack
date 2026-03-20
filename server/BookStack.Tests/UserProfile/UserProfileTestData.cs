namespace BookStack.Tests.UserProfile;

using System.Text;
using BookStack.Features.UserProfile.Service.Models;
using BookStack.Features.UserProfile.Web.User.Models;
using Microsoft.AspNetCore.Http;

internal static class UserProfileTestData
{
    public static CreateProfileServiceModel CreateServiceModel(
        string firstName = "Alice",
        string lastName = "Tester",
        IFormFile? image = null,
        bool removeImage = false)
        => new()
        {
            FirstName = firstName,
            LastName = lastName,
            Image = image,
            RemoveImage = removeImage,
        };

    public static CreateProfileWebModel CreateWebModel(
        string firstName = "Alice",
        string lastName = "Tester",
        IFormFile? image = null,
        bool removeImage = false)
        => new()
        {
            FirstName = firstName,
            LastName = lastName,
            Image = image,
            RemoveImage = removeImage,
        };

    public static IFormFile CreateImage(
        string fileName = "profile.jpg",
        string contentType = "image/jpeg",
        string content = "fake-image-content")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        return new FormFile(
            stream,
            0,
            bytes.Length,
            "image",
            fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}

