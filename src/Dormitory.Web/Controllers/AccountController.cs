using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Dormitory.Domain.Entities;
using Dormitory.Web.Models;
using Dormitory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dormitory.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly DormitoryContext _context;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, DormitoryContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    // ============ РЕЄСТРАЦІЯ ============
    [HttpGet]
    public IActionResult Register()
    {
        ViewData["FacultyId"] = new SelectList(_context.Faculties, "Facultyid", "Facultyname");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!string.IsNullOrEmpty(model.Phone))
        {
            model.Phone = model.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^(\+380|0)\d{9}$"))
                ModelState.AddModelError("Phone", "Невірний формат. Використовуйте 0XXXXXXXXX або +380XXXXXXXXX");
        }
        if (model.Birthdate.HasValue)
        {
            if (model.Birthdate < new DateOnly(1995, 1, 1) || model.Birthdate > new DateOnly(2009, 12, 31))
                ModelState.AddModelError("Birthdate", "Дата народження має бути між 1995 і 2009 роком");
        }
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
            ModelState.AddModelError("Email", "Користувач з таким email вже існує");

        if (ModelState.IsValid)
        {
            var student = new Student
            {
                Fullname = model.FullName, Course = model.Course, Birthdate = model.Birthdate,
                Address = model.Address, Phone = model.Phone, Email = model.Email,
                Gender = model.Gender, Facultyid = model.FacultyId,
                DistanceKm = model.DistanceKm, HasPrivilege = model.HasPrivilege
            };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var user = new User
            {
                Email = model.Email, UserName = model.Email,
                FullName = model.FullName, StudentId = student.Studentid
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "user");
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        ViewData["FacultyId"] = new SelectList(_context.Faculties, "Facultyid", "Facultyname", model.FacultyId);
        return View(model);
    }

    // ============ ВХІД ============
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Неправильний email або пароль");
        }
        return View(model);
    }

    // ============ ВИХІД ============
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ============ ПРОФІЛЬ ============
    [Authorize(Roles = "user")]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");
        Student? student = null;
        if (user.StudentId != null)
            student = await _context.Students.Include(s => s.Faculty)
                .FirstOrDefaultAsync(s => s.Studentid == user.StudentId);
        ViewBag.User = user;
        // Перевіряємо чи заселений
        if (user.StudentId != null)
        {
            var residence = await _context.Residencehistories
                .Include(r => r.Room)
                .Where(r => r.Studentid == user.StudentId && r.Checkoutdate == null)
                .FirstOrDefaultAsync();
            ViewBag.CurrentRoom = residence?.Room;
        }
        ViewBag.Student = student;
        // Перевірка обов'язкових документів
        if (user.StudentId != null)
        {
            var uploadedTypeNames = await _context.Documents
                .Include(d => d.Type)
                .Where(d => d.Studentid == user.StudentId)
                .Select(d => d.Type!.Typename)
                .ToListAsync();
            var mandatory = new[] { "Паспорт", "ІПН", "Флюорографія", "Довідка з місця проживання" };
            var missing = mandatory.Where(m => !uploadedTypeNames.Contains(m)).ToList();
            if (missing.Any())
                ViewBag.MissingDocs = missing;
        }
        return View();
    }

    [Authorize(Roles = "user")]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.StudentId == null)
        { TempData["Error"] = "Акаунт не прив'язаний до студента."; return RedirectToAction("Profile"); }
        var student = await _context.Students.Include(s => s.Faculty)
            .FirstOrDefaultAsync(s => s.Studentid == user.StudentId);
        if (student == null) return NotFound();
        ViewData["FacultyId"] = new SelectList(_context.Faculties, "Facultyid", "Facultyname", student.Facultyid);
        return View(student);
    }

    [Authorize(Roles = "user")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(string fullname, int? course, string address, string phone, string email, string gender, DateOnly? birthdate, int? facultyid)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.StudentId == null) return RedirectToAction("Profile");
        var student = await _context.Students.FindAsync(user.StudentId);
        if (student == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(fullname))
        {
            student.Fullname = fullname;
            user.FullName = fullname;
            await _userManager.UpdateAsync(user);
        }
        if (course.HasValue && course >= 1 && course <= 6)
            student.Course = course;
        if (!string.IsNullOrWhiteSpace(address))
            student.Address = address;
        if (!string.IsNullOrWhiteSpace(gender) && (gender == "Ч" || gender == "Ж"))
            student.Gender = gender;
        if (birthdate.HasValue)
            student.Birthdate = birthdate;
        if (facultyid.HasValue)
            student.Facultyid = facultyid;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(\+380|0)\d{9}$"))
            { TempData["Error"] = "Невірний формат телефону."; return RedirectToAction("EditProfile"); }
            student.Phone = phone;
        }
        if (!string.IsNullOrWhiteSpace(email))
        {
            student.Email = email;
            user.Email = email; user.UserName = email;
            await _userManager.UpdateAsync(user);
        }
        _context.Students.Update(student);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Профіль оновлено!";
        return RedirectToAction("Profile");
    }

    // ============================================================
    //  МОЇ ДОКУМЕНТИ — перегляд
    // ============================================================
    [Authorize(Roles = "user")]
    [HttpGet]
    public async Task<IActionResult> MyDocuments()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.StudentId == null)
        {
            TempData["Error"] = "Акаунт не прив'язаний до студента.";
            return RedirectToAction("Profile");
        }

        var studentId = user.StudentId.Value;

        // Документи студента
        var docs = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Type)
            .Where(d => d.Studentid == studentId)
            .OrderBy(d => d.Type!.Typename)
            .ToListAsync();

        // Всі типи документів
        var student = await _context.Students.FindAsync(studentId);
        var allTypes = await _context.DocumentTypes.AsNoTracking()
            .Where(t => !t.IsPrivilegeDoc || (student != null && student.HasPrivilege))
            .ToListAsync();

        // Типи, які студент ще не завантажив
        var uploadedTypeIds = docs.Select(d => d.Typeid).ToHashSet();
        // Якщо є хоча б один пільговий документ — решту пільгових не вважаємо відсутніми
        var hasAnyPrivilegeDoc = docs.Any(d => allTypes.Any(t => t.Typeid == d.Typeid && t.IsPrivilegeDoc));
        var missingTypes = allTypes
            .Where(t => !uploadedTypeIds.Contains(t.Typeid))
            .Where(t => !t.IsPrivilegeDoc || !hasAnyPrivilegeDoc)
            .ToList();

        ViewBag.Documents = docs;
        ViewBag.MissingTypes = missingTypes;
        ViewBag.AllTypes = allTypes;

        return View();
    }

    // ============================================================
    //  МОЇ ДОКУМЕНТИ — завантажити новий
    // ============================================================
    [Authorize(Roles = "user")]
    [HttpGet]
    public async Task<IActionResult> UploadDocument()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.StudentId == null) return RedirectToAction("MyDocuments");

        var studentId = user.StudentId.Value;
        var uploadedTypeIds = await _context.Documents
            .Where(d => d.Studentid == studentId)
            .Select(d => d.Typeid)
            .ToListAsync();

        var student = await _context.Students.FindAsync(studentId);
        var availableTypes = await _context.DocumentTypes
            .Where(t => !uploadedTypeIds.Contains(t.Typeid))
            .Where(t => !t.IsPrivilegeDoc || (student != null && student.HasPrivilege))
            .ToListAsync();
        if (!availableTypes.Any())
        {
            TempData["Success"] = "Всі документи вже завантажені!";
            return RedirectToAction("MyDocuments");
        }

        ViewData["Typeid"] = new SelectList(availableTypes, "Typeid", "Typename");
        // Передаємо інфо про типи для JS-валідації
        ViewBag.TypesJson = System.Text.Json.JsonSerializer.Serialize(
            availableTypes.Select(t => new { t.Typeid, t.RequiresIssueDate, t.IsLifetime }));

        return View();
    }

    [Authorize(Roles = "user")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(int typeid, DateTime? issuedate, DateTime? expirydate, IFormFile uploadedFile)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.StudentId == null) return RedirectToAction("MyDocuments");

        var studentId = user.StudentId.Value;

        // Перевірка: чи вже є такий тип
        var exists = await _context.Documents
            .AnyAsync(d => d.Studentid == studentId && d.Typeid == typeid);
        if (exists)
        {
            TempData["Error"] = "Цей тип документа вже завантажений.";
            return RedirectToAction("MyDocuments");
        }

        // Перевірка файлу
        if (uploadedFile == null || uploadedFile.Length == 0)
        {
            ModelState.AddModelError("", "Завантажте скан-копію документа!");
            return await ReturnUploadView();
        }

        // Перевірка дат за типом
        var docType = await _context.DocumentTypes.FindAsync(typeid);
        if (docType != null)
        {
            if (docType.RequiresIssueDate && !issuedate.HasValue)
                ModelState.AddModelError("", $"Для \"{docType.Typename}\" обов'язково вкажіть дату видачі.");

            if (!docType.IsLifetime && !expirydate.HasValue)
                ModelState.AddModelError("", $"\"{docType.Typename}\" не є довічним — вкажіть дату закінчення дії.");

            if (issuedate.HasValue && issuedate > DateTime.Today)
                ModelState.AddModelError("", "Дата видачі не може бути в майбутньому.");

            if (issuedate.HasValue && expirydate.HasValue && expirydate <= issuedate)
                ModelState.AddModelError("", "Дата закінчення має бути пізніше дати видачі.");

            // Флюорографія — рівно 1 рік
            if (docType.Typename == "Флюорографія" && issuedate.HasValue && expirydate.HasValue)
            {
                var expected = issuedate.Value.AddYears(1);
                if (expirydate.Value.Date != expected.Date)
                    ModelState.AddModelError("", $"Флюорографія дійсна рівно 1 рік — дата закінчення має бути {expected:dd.MM.yyyy}");
            }
        }

        if (!ModelState.IsValid)
            return await ReturnUploadView();

        // Зберігаємо
        using var memoryStream = new MemoryStream();
        await uploadedFile.CopyToAsync(memoryStream);

        var document = new Document
        {
            Studentid = studentId,
            Typeid = typeid,
            Filecontent = memoryStream.ToArray(),
            Issuedate = issuedate,
            Expirydate = expirydate,
            Uploaddate = DateTime.Now
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Документ \"{docType?.Typename}\" успішно завантажено!";
        return RedirectToAction("MyDocuments");
    }

    // ============================================================
    //  МОЇ ДОКУМЕНТИ — видалити
    // ============================================================
    [Authorize(Roles = "user")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.StudentId == null) return RedirectToAction("MyDocuments");

        var doc = await _context.Documents.FindAsync(id);
        if (doc == null || doc.Studentid != user.StudentId)
            return NotFound();

        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Документ видалено.";
        return RedirectToAction("MyDocuments");
    }

    // ============================================================
    //  МОЇ ДОКУМЕНТИ — завантажити файл
    // ============================================================
    [Authorize(Roles = "user")]
    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.StudentId == null) return NotFound();

        var doc = await _context.Documents
            .Include(d => d.Type)
            .FirstOrDefaultAsync(d => d.Documentid == id && d.Studentid == user.StudentId);

        if (doc?.Filecontent == null || doc.Filecontent.Length == 0)
            return NotFound();

        string contentType = "application/octet-stream";
        string ext = "bin";
        if (doc.Filecontent.Length > 3)
        {
            if (doc.Filecontent[0] == 0x25 && doc.Filecontent[1] == 0x50) { contentType = "application/pdf"; ext = "pdf"; }
            else if (doc.Filecontent[0] == 0xFF && doc.Filecontent[1] == 0xD8) { contentType = "image/jpeg"; ext = "jpg"; }
            else if (doc.Filecontent[0] == 0x89 && doc.Filecontent[1] == 0x50) { contentType = "image/png"; ext = "png"; }
        }
        return File(doc.Filecontent, contentType, $"{doc.Type?.Typename ?? "doc"}_{id}.{ext}");
    }

    // Helper
    private async Task<IActionResult> ReturnUploadView()
    {
        var user = await _userManager.GetUserAsync(User);
        var studentId = user!.StudentId!.Value;
        var uploadedTypeIds = await _context.Documents
            .Where(d => d.Studentid == studentId).Select(d => d.Typeid).ToListAsync();
        var student = await _context.Students.FindAsync(studentId);
        var availableTypes = await _context.DocumentTypes
            .Where(t => !uploadedTypeIds.Contains(t.Typeid))
            .Where(t => !t.IsPrivilegeDoc || (student != null && student.HasPrivilege))
            .ToListAsync();
        ViewData["Typeid"] = new SelectList(availableTypes, "Typeid", "Typename");
        ViewBag.TypesJson = System.Text.Json.JsonSerializer.Serialize(
            availableTypes.Select(t => new { t.Typeid, t.RequiresIssueDate, t.IsLifetime }));
        return View("UploadDocument");
    }

    // ============ ЗМІНА ПАРОЛЯ ============

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        if (model.OldPassword == model.NewPassword)
        {
            ModelState.AddModelError("", "Новий пароль не може збігатися з поточним.");
            return View(model);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Пароль успішно змінено!";
            return RedirectToAction("ChangePassword");
        }
        else
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }
}
