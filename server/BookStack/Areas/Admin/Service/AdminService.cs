namespace BookStack.Areas.Admin.Service;

using Features.Identity.Data.Models;
using Microsoft.AspNetCore.Identity;

using static Common.Constants.Names;

public class AdminService(
    UserManager<UserDbModel> userManager) : IAdminService
{
    public async Task<IEnumerable<string>> GetIds()
    {
        var admins = await userManager
            .GetUsersInRoleAsync(AdminRoleName);

        return [.. admins.Select(a => a.Id)];
    }
}
