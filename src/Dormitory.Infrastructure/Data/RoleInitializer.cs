using Microsoft.AspNetCore.Identity;
using Dormitory.Domain.Entities;

namespace Dormitory.Infrastructure.Data;

public class RoleInitializer
{
    public static async Task InitializeAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Створюємо ролі
        if (await roleManager.FindByNameAsync("superadmin") == null)
            await roleManager.CreateAsync(new IdentityRole("superadmin"));

        if (await roleManager.FindByNameAsync("admin") == null)
            await roleManager.CreateAsync(new IdentityRole("admin"));

        if (await roleManager.FindByNameAsync("user") == null)
            await roleManager.CreateAsync(new IdentityRole("user"));

        // Головний адмін — Петренко Іван
        string adminEmail = "ipetrenko@gmail.com";
        string adminPassword = "Qwerty_123";

        if (await userManager.FindByNameAsync(adminEmail) == null)
        {
            User admin = new User
            {
                Email = adminEmail,
                UserName = adminEmail,
                FullName = "Петренко Іван"
            };

            IdentityResult result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "superadmin");
        }
        // Адмін — Іванов Петро
string admin2Email = "p_ivanov@gmail.com";
string admin2Password = "Qwerty_123";

if (await userManager.FindByNameAsync(admin2Email) == null)
{
    User admin2 = new User
    {
        Email = admin2Email,
        UserName = admin2Email,
        FullName = "Іванов Петро"
    };

    IdentityResult result2 = await userManager.CreateAsync(admin2, admin2Password);
    if (result2.Succeeded)
        await userManager.AddToRoleAsync(admin2, "admin");
}
        else
        {
            // Якщо вже існує — переконатись що має роль superadmin
            var existingAdmin = await userManager.FindByNameAsync(adminEmail);
            if (existingAdmin != null && !await userManager.IsInRoleAsync(existingAdmin, "superadmin"))
            {
                await userManager.AddToRoleAsync(existingAdmin, "superadmin");
            }
            // Прибрати стару роль admin якщо є
            if (existingAdmin != null && await userManager.IsInRoleAsync(existingAdmin, "admin"))
            {
                await userManager.RemoveFromRoleAsync(existingAdmin, "admin");
            }
        }
    }
}
