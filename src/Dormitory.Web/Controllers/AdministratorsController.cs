using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;

namespace Dormitory.Web.Controllers
{
    [Authorize(Roles = "admin,superadmin")]
    public class AdministratorsController : Controller
    {
        private readonly DormitoryContext _context;
        private readonly UserManager<User> _userManager;

        public AdministratorsController(DormitoryContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Administrators.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var administrator = await _context.Administrators
                .FirstOrDefaultAsync(m => m.Adminid == id);
            if (administrator == null) return NotFound();

            return View(administrator);
        }

        // Тільки superadmin може додавати адмінів
        [Authorize(Roles = "superadmin")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Adminid,Username,Fullname")] Administrator administrator, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                ModelState.AddModelError("", "Емайл обов'язковий.");
            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("", "Пароль обов'язковий.");

            if (await _userManager.FindByEmailAsync(email) != null)
                ModelState.AddModelError("", "Користувач з таким email вже існує.");

            if (ModelState.IsValid)
            {
                // Створюємо користувача в Identity
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FullName = administrator.Fullname
                };
                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(administrator);
                }

                await _userManager.AddToRoleAsync(user, "admin");

                // Створюємо запис в Administrators
                _context.Add(administrator);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(administrator);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var administrator = await _context.Administrators.FindAsync(id);
            if (administrator == null) return NotFound();

            // Перевіряємо що адмін редагує тільки себе (superadmin може всіх)
            if (!User.IsInRole("superadmin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || currentUser.UserName != administrator.Username)
                    return Forbid();
            }

            return View(administrator);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Adminid,Username,Fullname")] Administrator administrator)
        {
            if (id != administrator.Adminid) return NotFound();

            // Перевіряємо що адмін редагує тільки себе (superadmin може всіх)
            if (!User.IsInRole("superadmin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || currentUser.UserName != administrator.Username)
                    return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(administrator);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdministratorExists(administrator.Adminid))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(administrator);
        }

        // Тільки superadmin може видаляти адмінів
        [Authorize(Roles = "superadmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var administrator = await _context.Administrators
                .FirstOrDefaultAsync(m => m.Adminid == id);
            if (administrator == null) return NotFound();

            // Заборона видалення самого себе
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && administrator.Username == currentUser.UserName)
            {
                TempData["Error"] = "Ви не можете видалити самого себе.";
                return RedirectToAction(nameof(Index));
            }

            // Заборона видалення будь-якого superadmin
            var targetUser = await _userManager.FindByNameAsync(administrator.Username);
            if (targetUser != null && await _userManager.IsInRoleAsync(targetUser, "superadmin"))
            {
                TempData["Error"] = "Неможливо видалити суперадміністратора.";
                return RedirectToAction(nameof(Index));
            }

            return View(administrator);
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var administrator = await _context.Administrators.FindAsync(id);
            if (administrator == null)
                return RedirectToAction(nameof(Index));

            // Подвійна перевірка — заборона видалення будь-якого superadmin
            var targetUser2 = await _userManager.FindByNameAsync(administrator.Username);
            if (targetUser2 != null && await _userManager.IsInRoleAsync(targetUser2, "superadmin"))
            {
                TempData["Error"] = "Неможливо видалити суперадміністратора.";
                return RedirectToAction(nameof(Index));
            }


            // Подвійна перевірка — заборона видалення самого себе
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && administrator.Username == currentUser.UserName)
            {
                TempData["Error"] = "Ви не можете видалити самого себе.";
                return RedirectToAction(nameof(Index));
            }

            // Перевірка чи є пов'язані заявки
            bool hasApplications = await _context.Applications
                .AnyAsync(a => a.Adminid == id);

            if (hasApplications)
            {
                TempData["Error"] = "Неможливо видалити адміністратора — він прив'язаний до заявок.";
                return RedirectToAction(nameof(Index));
            }

            _context.Administrators.Remove(administrator);
            await _context.SaveChangesAsync();

            // Видаляємо користувача з Identity
            if (targetUser2 != null)
            {
                await _userManager.DeleteAsync(targetUser2);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AdministratorExists(int id)
        {
            return _context.Administrators.Any(e => e.Adminid == id);
        }
    }
}