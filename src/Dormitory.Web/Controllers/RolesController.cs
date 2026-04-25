using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Dormitory.Domain.Entities;
using Dormitory.Web.Models;

namespace Dormitory.Web.Controllers;

[Authorize(Roles = "superadmin")]
public class RolesController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<User> _userManager;

    public RolesController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public IActionResult Index() => View(_roleManager.Roles.ToList());
    public IActionResult UserList() => View(_userManager.Users.ToList());

    public async Task<IActionResult> Edit(string userId)
    {
        User? user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        var userRoles = await _userManager.GetRolesAsync(user);
        var model = new ChangeRoleViewModel
        {
            UserId = user.Id,
            UserEmail = user.Email ?? "",
            UserRoles = userRoles,
            AllRoles = _roleManager.Roles.ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string userId, List<string> roles)
    {
        User? user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        // Захист: не можна зняти superadmin з себе
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == userId && !roles.Contains("superadmin"))
        {
            TempData["Error"] = "Ви не можете зняти роль суперадміністратора з себе.";
            return RedirectToAction("Edit", new { userId });
        }

        // Захист: звичайний користувач не може призначити роль superadmin
        if (roles.Contains("superadmin") && currentUser?.Id != userId)
        {
            var currentUserIsSuperAdmin = await _userManager.IsInRoleAsync(currentUser!, "superadmin");
            if (!currentUserIsSuperAdmin)
            {
                TempData["Error"] = "Тільки суперадміністратор може призначати цю роль.";
                return RedirectToAction("Edit", new { userId });
            }
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        await _userManager.AddToRolesAsync(user, roles.Except(userRoles));
        await _userManager.RemoveFromRolesAsync(user, userRoles.Except(roles));

        return RedirectToAction("UserList");
    }
}
