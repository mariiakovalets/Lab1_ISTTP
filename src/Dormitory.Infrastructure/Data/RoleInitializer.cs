using Microsoft.AspNetCore.Identity;
using Dormitory.Domain.Entities;

namespace Dormitory.Infrastructure.Data;

public class RoleInitializer
{
    public static async Task InitializeAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
/*         string adminEmail = "admin@dormitory.com";
        string password   = "Admin_1234";

        if (await roleManager.FindByNameAsync("admin") == null)
            await roleManager.CreateAsync(new IdentityRole("admin"));

        if (await roleManager.FindByNameAsync("user") == null)
            await roleManager.CreateAsync(new IdentityRole("user"));

        if (await userManager.FindByNameAsync(adminEmail) == null)
        {
            User admin = new User
            {
                Email    = adminEmail,
                UserName = adminEmail,
                FullName = "Головний адміністратор"
            };

            IdentityResult result = await userManager.CreateAsync(admin, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "admin");
        } */

        // Другий адмін — Петренко Іван
        string admin2Email = "ipetrenko@gmail.com";
        string admin2Password = "Qwerty_123";

        if (await userManager.FindByNameAsync(admin2Email) == null)
        {
            User admin2 = new User
            {
                Email = admin2Email,
                UserName = admin2Email,
                FullName = "Петренко Іван"
            };

            IdentityResult result2 = await userManager.CreateAsync(admin2, admin2Password);
            if (result2.Succeeded)
                await userManager.AddToRoleAsync(admin2, "admin");
        }
    }
}