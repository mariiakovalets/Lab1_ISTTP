using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Dormitory.Domain.Entities;
using Dormitory.Web.Models;

namespace Dormitory.Web.Controllers;

[Authorize(Roles = "admin")]
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
        var userRoles = await _userManager.GetRolesAsync(user);
        await _userManager.AddToRolesAsync(user, roles.Except(userRoles));
        await _userManager.RemoveFromRolesAsync(user, userRoles.Except(roles));
        return RedirectToAction("UserList");
    }
}
