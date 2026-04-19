using Microsoft.AspNetCore.Identity;

namespace Dormitory.Web.Models;

public class ChangeRoleViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public List<IdentityRole> AllRoles { get; set; } = new();
    public IList<string> UserRoles { get; set; } = new List<string>();
}
