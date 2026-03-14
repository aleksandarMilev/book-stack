namespace BookStack.Features.Identity.Data.Models;

using BookStack.Data.Models.Base;
using Microsoft.AspNetCore.Identity;
using UserProfile.Data.Models;

public class UserDbModel :
    IdentityUser,
    IDeletableEntity
{
    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedOn { get; set; }

    public string? DeletedBy { get; set; }

    public UserProfileDbModel? Profile { get; init; }
}
