namespace BookStack.Features.UserProfile.Service.Models;

using Infrastructure.Services.ImageWriter.Models;

public class CreateProfileServiceModel : IImageServiceModel
{
    public string FirstName { get; init; } = default!;

    public string LastName { get; init; } = default!;

    public IFormFile? Image { get; init; }

    public bool RemoveImage { get; init; }
}
