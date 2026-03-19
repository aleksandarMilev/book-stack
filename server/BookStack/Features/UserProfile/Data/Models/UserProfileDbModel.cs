namespace BookStack.Features.UserProfile.Data.Models;

using BookStack.Data.Models.Base;
using Identity.Data.Models;
using Infrastructure.Services.ImageWriter.Models;

public class UserProfileDbModel:
    IDeletableEntity,
    IImageDbModel
{
    public bool IsDeleted { get; set; }

    public DateTime? DeletedOn { get; set; }

    public string? DeletedBy { get; set; }

    public string UserId { get; set; } = default!;

    public UserDbModel User { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string ImagePath { get; set; } = default!;

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }
}
