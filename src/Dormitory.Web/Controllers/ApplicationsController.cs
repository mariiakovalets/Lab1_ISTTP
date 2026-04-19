using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Dormitory.Infrastructure.Services;

namespace Dormitory.Web.Controllers
{
    [Authorize]
    public class ApplicationsController : Controller
    {
        private readonly DormitoryContext _context;
        private readonly ApplicationDataPortServiceFactory _applicationDataPortServiceFactory;
        private readonly UserManager<User> _userManager;

        public ApplicationsController(DormitoryContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
            _applicationDataPortServiceFactory = new ApplicationDataPortServiceFactory(context);
        }

        // ============ ADMIN ============

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var apps = _context.Applications.AsNoTracking()
                .Include(a => a.Admin).Include(a => a.Status).Include(a => a.Student)
                .OrderByDescending(a => a.Submissiondate);
            return View(await apps.ToListAsync());
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var app = await _context.Applications
                .Include(a => a.Admin).Include(a => a.Status).Include(a => a.Student)
                .FirstOrDefaultAsync(m => m.Applicationid == id);
            if (app == null) return NotFound();
            return View(app);
        }

        [Authorize(Roles = "admin")]
        public IActionResult Create()
        {
            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username");
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname");
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([Bind("Applicationid,Studentid,Statusid,Applicationtype,Submissiondate,Decisiondate,Rejectionreason,Extensionstartdate,Extensionenddate,Adminid,Academicperiod")] Application application)
        {
            if (application.Applicationtype == "Поселення")
            {
                var settled = await _context.Residencehistories.AnyAsync(r => r.Studentid == application.Studentid && r.Checkoutdate == null);
                if (settled) ModelState.AddModelError("Studentid", "Цей студент вже заселений!");
            }
            ValidateDates(application);
            if (ModelState.IsValid)
            {
                _context.Add(application);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username", application.Adminid);
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname", application.Statusid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", application.Studentid);
            return View(application);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var app = await _context.Applications.FindAsync(id);
            if (app == null) return NotFound();
            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username", app.Adminid);
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname", app.Statusid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", app.Studentid);
            return View(app);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Applicationid,Studentid,Statusid,Applicationtype,Submissiondate,Decisiondate,Rejectionreason,Extensionstartdate,Extensionenddate,Adminid,Academicperiod")] Application application)
        {
            if (id != application.Applicationid) return NotFound();
            if (application.Applicationtype == "Поселення")
            {
                if (await _context.Residencehistories.AnyAsync(r => r.Studentid == application.Studentid && r.Checkoutdate == null))
                    ModelState.AddModelError("Studentid", "Цей студент вже заселений!");
                if (await _context.Applications.AnyAsync(a => a.Studentid == application.Studentid && a.Applicationtype == "Поселення" && a.Applicationid != application.Applicationid && a.Decisiondate == null))
                    ModelState.AddModelError("Studentid", "Активна заява вже є!");
            }
            ValidateDates(application);
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(application);
                    await _context.SaveChangesAsync();
                    if (application.Statusid == 3)
                    {
                        var qe = await _context.Queues.FirstOrDefaultAsync(q => q.Applicationid == application.Applicationid);
                        if (qe != null) { _context.Queues.Remove(qe); await _context.SaveChangesAsync(); var rem = await _context.Queues.OrderBy(q => q.Position).ToListAsync(); for (int i = 0; i < rem.Count; i++) rem[i].Position = i + 1; await _context.SaveChangesAsync(); }
                    }
                    if (application.Statusid == 2 && application.Applicationtype == "Поселення")
                    {
                        if (!await _context.Queues.AnyAsync(q => q.Applicationid == application.Applicationid) && !await _context.Residencehistories.AnyAsync(r => r.Studentid == application.Studentid && r.Checkoutdate == null))
                        {
                            if (!await TryAssignRoom(application))
                            { var s = await _context.Students.FindAsync(application.Studentid); await RecalculateQueuePositions(application.Applicationid, s); }
                        }
                    }
                }
                catch (DbUpdateConcurrencyException) { if (!ApplicationExists(application.Applicationid)) return NotFound(); else throw; }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username", application.Adminid);
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname", application.Statusid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", application.Studentid);
            return View(application);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var app = await _context.Applications.Include(a => a.Admin).Include(a => a.Status).Include(a => a.Student).FirstOrDefaultAsync(m => m.Applicationid == id);
            if (app == null) return NotFound();
            return View(app);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var app = await _context.Applications.FindAsync(id);
            if (app != null) _context.Applications.Remove(app);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "admin")]
        public IActionResult Import() => View();

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "admin")]
        public async Task<IActionResult> Import(IFormFile fileExcel, CancellationToken ct)
        {
            ModelState.Remove("fileExcel");
            if (fileExcel == null || fileExcel.Length == 0) { ModelState.AddModelError("", "Оберіть файл"); return View(); }
            const string xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            if (fileExcel.ContentType != xlsx) { ModelState.AddModelError("", "Лише .xlsx"); return View(); }
            try { var svc = _applicationDataPortServiceFactory.GetImportService(fileExcel.ContentType); using var s = fileExcel.OpenReadStream(); await svc.ImportFromStreamAsync(s, ct); return RedirectToAction(nameof(Index)); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(); }
            catch { ModelState.AddModelError("", "Помилка при імпорті."); return View(); }
        }

        [HttpGet, Authorize(Roles = "admin")]
        public async Task<IActionResult> Export(CancellationToken ct)
        {
            const string c = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var svc = _applicationDataPortServiceFactory.GetExportService(c);
            var ms = new MemoryStream(); await svc.WriteToAsync(ms, ct); await ms.FlushAsync(ct); ms.Position = 0;
            return new FileStreamResult(ms, c) { FileDownloadName = "applications_export.xlsx" };
        }

        // ============================================================
        //  СТУДЕНТ: мої заяви
        // ============================================================
        [Authorize(Roles = "user")]
        public async Task<IActionResult> MyApplications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.StudentId == null)
            {
                ViewBag.Message = "Ваш акаунт не прив'язаний до жодного студента.";
                return View(new List<Application>());
            }
            var studentId = user.StudentId.Value;
            var apps = await _context.Applications.AsNoTracking()
                .Include(a => a.Status)
                .Where(a => a.Studentid == studentId)
                .OrderByDescending(a => a.Submissiondate)
                .ToListAsync();
            return View(apps);
        }

        // ============================================================
        //  СТУДЕНТ: подати заяву — GET
        // ============================================================
        [Authorize(Roles = "user")]
        [HttpGet]
        public async Task<IActionResult> StudentCreate()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.StudentId == null)
            {
                TempData["Error"] = "Акаунт не прив'язаний до студента.";
                return RedirectToAction("MyApplications");
            }

            var studentId = user.StudentId.Value;

            // Перевірка активної заяви
            var hasActive = await _context.Applications
                .AnyAsync(a => a.Studentid == studentId && (a.Statusid == 1 || a.Statusid == 2));
            if (hasActive)
            {
                TempData["Error"] = "У вас вже є активна заява.";
                return RedirectToAction("MyApplications");
            }

            var student = await _context.Students.FindAsync(studentId);
            var isSettled = await _context.Residencehistories
                .AnyAsync(r => r.Studentid == studentId && r.Checkoutdate == null);

            // Документи студента
            var uploadedDocs = await _context.Documents.AsNoTracking()
                .Include(d => d.Type)
                .Where(d => d.Studentid == studentId)
                .ToListAsync();

            // Потрібні типи документів (без пільгових якщо немає пільги)
            var requiredTypes = await _context.DocumentTypes.AsNoTracking()
                .Where(t => !t.IsPrivilegeDoc || (student != null && student.HasPrivilege))
                .ToListAsync();

            var uploadedTypeIds = uploadedDocs.Select(d => d.Typeid).ToHashSet();
            var missingTypes = requiredTypes.Where(t => !uploadedTypeIds.Contains(t.Typeid)).ToList();

            // Обов'язковий мінімум: Паспорт, ІПН, Флюорографія, Довідка з місця проживання
            var mandatoryNames = new[] { "Паспорт", "ІПН", "Флюорографія", "Довідка з місця проживання" };
            var missingMandatory = requiredTypes
                .Where(t => mandatoryNames.Contains(t.Typename) && !uploadedTypeIds.Contains(t.Typeid))
                .Select(t => t.Typename).ToList();

            // Перевірка прострочених
            var expiredDocs = uploadedDocs
                .Where(d => d.Expirydate.HasValue && d.Expirydate.Value < DateTime.Today)
                .Select(d => d.Type?.Typename ?? "Невідомий").ToList();

            ViewBag.IsSettled = isSettled;
            ViewBag.Student = student;
            ViewBag.UploadedDocs = uploadedDocs;
            ViewBag.RequiredTypes = requiredTypes;
            ViewBag.MissingTypes = missingTypes;
            ViewBag.MissingMandatory = missingMandatory;
            ViewBag.ExpiredDocs = expiredDocs;
            ViewBag.HasPrivilege = student?.HasPrivilege ?? false;

            // Для завантаження документів прямо тут
            ViewBag.AvailableTypesForUpload = missingTypes;
            ViewBag.TypesJson = System.Text.Json.JsonSerializer.Serialize(
                missingTypes.Select(t => new { t.Typeid, t.RequiresIssueDate, t.IsLifetime, t.IsPrivilegeDoc, t.Typename }));

            return View();
        }

        // ============================================================
        //  СТУДЕНТ: завантажити документ (з форми заяви)
        // ============================================================
        [Authorize(Roles = "user")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocumentInline(int typeid, DateTime? issuedate, DateTime? expirydate, IFormFile uploadedFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.StudentId == null) return RedirectToAction("StudentCreate");

            var studentId = user.StudentId.Value;

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["Error"] = "Оберіть файл для завантаження.";
                return RedirectToAction("StudentCreate");
            }

            var exists = await _context.Documents.AnyAsync(d => d.Studentid == studentId && d.Typeid == typeid);
            if (exists)
            {
                TempData["Error"] = "Цей тип документа вже завантажено.";
                return RedirectToAction("StudentCreate");
            }

            var docType = await _context.DocumentTypes.FindAsync(typeid);
            if (docType != null)
            {
                if (docType.RequiresIssueDate && !issuedate.HasValue)
                { TempData["Error"] = $"Для \"{docType.Typename}\" обов'язково вкажіть дату видачі."; return RedirectToAction("StudentCreate"); }

                if (!docType.IsLifetime && !expirydate.HasValue)
                { TempData["Error"] = $"\"{docType.Typename}\" — вкажіть дату закінчення дії."; return RedirectToAction("StudentCreate"); }

                if (issuedate.HasValue && issuedate > DateTime.Today)
                { TempData["Error"] = "Дата видачі не може бути в майбутньому."; return RedirectToAction("StudentCreate"); }

                if (issuedate.HasValue && expirydate.HasValue && expirydate <= issuedate)
                { TempData["Error"] = "Дата закінчення має бути пізніше дати видачі."; return RedirectToAction("StudentCreate"); }

                if (docType.Typename == "Флюорографія" && issuedate.HasValue && expirydate.HasValue)
                {
                    var expected = issuedate.Value.AddYears(1);
                    if (expirydate.Value.Date != expected.Date)
                    { TempData["Error"] = $"Флюорографія дійсна 1 рік — дата закінчення має бути {expected:dd.MM.yyyy}"; return RedirectToAction("StudentCreate"); }
                }
            }

            using var ms = new MemoryStream();
            await uploadedFile.CopyToAsync(ms);

            _context.Documents.Add(new Document
            {
                Studentid = studentId,
                Typeid = typeid,
                Filecontent = ms.ToArray(),
                Issuedate = issuedate,
                Expirydate = expirydate,
                Uploaddate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Документ \"{docType?.Typename}\" завантажено!";
            return RedirectToAction("StudentCreate");
        }

        // ============================================================
        //  СТУДЕНТ: подати заяву — POST
        // ============================================================
        [Authorize(Roles = "user")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentCreate(string applicationtype, string academicperiod)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.StudentId == null) return RedirectToAction("MyApplications");

            var studentId = user.StudentId.Value;
            var student = await _context.Students.FindAsync(studentId);

            if (string.IsNullOrWhiteSpace(applicationtype))
            { TempData["Error"] = "Оберіть тип заяви."; return RedirectToAction("StudentCreate"); }
            if (string.IsNullOrWhiteSpace(academicperiod))
            { TempData["Error"] = "Вкажіть академічний період."; return RedirectToAction("StudentCreate"); }

            var isSettled = await _context.Residencehistories
                .AnyAsync(r => r.Studentid == studentId && r.Checkoutdate == null);

            if (applicationtype == "Пролонгація" && !isSettled)
            { TempData["Error"] = "Пролонгацію можна подати тільки якщо ви вже заселені."; return RedirectToAction("StudentCreate"); }
            if (applicationtype == "Поселення" && isSettled)
            { TempData["Error"] = "Ви вже заселені. Подайте заяву на пролонгацію."; return RedirectToAction("StudentCreate"); }

            // Перевірка обов'язкових документів
            var uploadedTypeIds = await _context.Documents
                .Where(d => d.Studentid == studentId).Select(d => d.Typeid).ToListAsync();

            var mandatoryTypes = await _context.DocumentTypes
                .Where(t => new[] { "Паспорт", "ІПН", "Флюорографія", "Довідка з місця проживання" }.Contains(t.Typename))
                .ToListAsync();

            var missingMandatory = mandatoryTypes
                .Where(t => !uploadedTypeIds.Contains(t.Typeid))
                .Select(t => t.Typename).ToList();

            if (missingMandatory.Any())
            {
                TempData["Error"] = $"Спочатку завантажте обов'язкові документи: {string.Join(", ", missingMandatory)}";
                return RedirectToAction("StudentCreate");
            }

            // Перевірка прострочених
            var expiredDocs = await _context.Documents.Include(d => d.Type)
                .Where(d => d.Studentid == studentId && d.Expirydate.HasValue && d.Expirydate.Value < DateTime.Today)
                .Select(d => d.Type!.Typename).ToListAsync();

            if (expiredDocs.Any())
            {
                TempData["Error"] = $"У вас є прострочені документи: {string.Join(", ", expiredDocs)}. Оновіть їх перед подачею.";
                return RedirectToAction("StudentCreate");
            }

            // Перевірка пільгового документа
            if (student?.HasPrivilege == true)
            {
                var hasPrivDoc = await _context.Documents.Include(d => d.Type)
                    .AnyAsync(d => d.Studentid == studentId && d.Type!.IsPrivilegeDoc);
                if (!hasPrivDoc)
                {
                    TempData["Error"] = "Ви вказали наявність пільги — завантажте документ, що її підтверджує.";
                    return RedirectToAction("StudentCreate");
                }
            }

            var pendingStatus = await _context.Applicationstatuses
                .FirstOrDefaultAsync(s => s.Statusname == "На розгляді");

            _context.Applications.Add(new Application
            {
                Studentid = studentId,
                Statusid = pendingStatus?.Statusid ?? 1,
                Applicationtype = applicationtype,
                Submissiondate = DateTime.Now,
                Academicperiod = academicperiod
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Заяву успішно подано!";
            return RedirectToAction("MyApplications");
        }

        // ============ Private helpers ============

        private async Task<bool> TryAssignRoom(Application application)
        {
            var student = await _context.Students.FindAsync(application.Studentid);
            if (student == null) return false;
            if (await _context.Residencehistories.AnyAsync(r => r.Studentid == application.Studentid && r.Checkoutdate == null)) return false;
            var rooms = await _context.Rooms.ToListAsync();
            foreach (var room in rooms)
            {
                var cur = await _context.Residencehistories.Include(r => r.Student).Where(r => r.Roomid == room.Roomid && r.Checkoutdate == null).ToListAsync();
                if (cur.Count >= room.Capacity) continue;
                if (cur.Any() && cur.First().Student?.Gender != student.Gender) continue;
                _context.Residencehistories.Add(new Residencehistory { Studentid = application.Studentid, Roomid = room.Roomid, Checkindate = DateTime.Today });
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        private async Task RecalculateQueuePositions(int newAppId, Student? newStudent)
        {
            if (newStudent?.Studentid != null) newStudent = await _context.Students.FindAsync(newStudent.Studentid);
            var entries = await _context.Queues.Include(q => q.Application).ThenInclude(a => a!.Student).ToListAsync();
            entries.Add(new Queue { Applicationid = newAppId, Application = new Application { Applicationid = newAppId, Student = newStudent } });
            var sorted = entries.OrderByDescending(q => q.Application?.Student?.HasPrivilege ?? false).ThenByDescending(q => q.Application?.Student?.DistanceKm ?? 0).ToList();
            _context.Queues.RemoveRange(await _context.Queues.ToListAsync());
            for (int i = 0; i < sorted.Count; i++) _context.Queues.Add(new Queue { Applicationid = sorted[i].Applicationid, Position = i + 1 });
            await _context.SaveChangesAsync();
        }

        private void ValidateDates(Application app)
        {
            if (app.Submissiondate.HasValue && app.Submissiondate > DateTime.Now) ModelState.AddModelError("Submissiondate", "Дата подачі не може бути в майбутньому");
            if (app.Submissiondate.HasValue && app.Submissiondate < new DateTime(2010, 1, 1)) ModelState.AddModelError("Submissiondate", "Дата подачі не може бути раніше 2010");
            if (app.Decisiondate.HasValue && app.Decisiondate > DateTime.Now) ModelState.AddModelError("Decisiondate", "Дата рішення не може бути в майбутньому");
            if (app.Decisiondate.HasValue && app.Submissiondate.HasValue)
            {
                if (app.Decisiondate < app.Submissiondate) ModelState.AddModelError("Decisiondate", "Дата рішення раніше дати подачі");
                if (app.Decisiondate > app.Submissiondate.Value.AddDays(5)) ModelState.AddModelError("Decisiondate", "Рішення пізніше ніж через 5 днів");
            }
            if (app.Extensionstartdate.HasValue && app.Extensionenddate.HasValue && app.Extensionenddate < app.Extensionstartdate)
                ModelState.AddModelError("Extensionenddate", "Дата кінця раніше дати початку");
        }

        private bool ApplicationExists(int id) => _context.Applications.Any(e => e.Applicationid == id);
    }
}
