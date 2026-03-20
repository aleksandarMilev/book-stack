namespace BookStack.Features.SellerProfiles.Web.Models;

/// <summary>
/// Request payload used by administrator endpoints to activate or deactivate a seller profile.
/// </summary>
public class ChangeSellerProfileStatusWebModel
{
    /// <summary>
    /// Desired seller-profile activation state.
    /// </summary>
    public bool IsActive { get; init; }
}
