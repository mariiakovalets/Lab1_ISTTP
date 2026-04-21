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

        // ============ ADMIN: список заяв ============
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var apps = await _context.Applications.AsNoTracking()
                .Include(a => a.Admin).Include(a => a.Status).Include(a => a.Student)
                .OrderByDescending(a => a.Submissiondate).ToListAsync();
            return View(apps);
        }

        // ============ ADMIN: деталі заяви + документи ============
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var app = await _context.Applications
                .Include(a => a.Admin).Include(a => a.Status)
                .Include(a => a.Student).ThenInclude(s => s!.Faculty)
                .FirstOrDefaultAsync(m => m.Applicationid == id);
            if (app == null) return NotFound();

            await LoadStudentDocuments(app.Studentid);
            return View(app);
        }

        // ============ ADMIN: розгляд заяви ============
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Review(int? id)
        {
            if (id == null) return NotFound();
            var app = await _context.Applications
                .Include(a => a.Admin).Include(a => a.Status)
                .Include(a => a.Student).ThenInclude(s => s!.Faculty)
                .FirstOrDefaultAsync(m => m.Applicationid == id);
            if (app == null) return NotFound();
            if (app.Status?.Statusname != "На розгляді")
            {
                TempData["Error"] = "Ця заява вже розглянута.";
                return RedirectToAction("Details", new { id });
            }

            await LoadStudentDocuments(app.Studentid);
            return View(app);
        }

        // ============ ADMIN: схвалити ============
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var app = await _context.Applications
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Applicationid == id);
            if (app == null) return NotFound();

            // Перевірка документів
            var studentId = app.Studentid ?? 0;
            var student = await _context.Students.FindAsync(studentId);
            var uploadedTypeIds = await _context.Documents
                .Where(d => d.Studentid == studentId).Select(d => d.Typeid).ToListAsync();

            var mandatoryTypes = await _context.DocumentTypes
                .Where(t => new[] { "Паспорт", "ІПН", "Флюорографія", "Довідка з місця проживання" }.Contains(t.Typename))
                .ToListAsync();
            var missing = mandatoryTypes.Where(t => !uploadedTypeIds.Contains(t.Typeid)).Select(t => t.Typename).ToList();

            if (missing.Any())
            {
                TempData["Error"] = $"Неможливо схвалити — відсутні документи: {string.Join(", ", missing)}";
                return RedirectToAction("Review", new { id });
            }

            var expired = await _context.Documents.Include(d => d.Type)
                .Where(d => d.Studentid == studentId && d.Expirydate.HasValue && d.Expirydate.Value < DateTime.Today)
                .Select(d => d.Type!.Typename).ToListAsync();
            if (expired.Any())
            {
                TempData["Error"] = $"Неможливо схвалити — прострочені: {string.Join(", ", expired)}";
                return RedirectToAction("Review", new { id });
            }

            if (student?.HasPrivilege == true)
            {
                var hasPrivDoc = await _context.Documents.Include(d => d.Type)
                    .AnyAsync(d => d.Studentid == studentId && d.Type!.IsPrivilegeDoc);
                if (!hasPrivDoc)
                {
                    TempData["Error"] = "Студент має пільгу але пільговий документ відсутній.";
                    return RedirectToAction("Review", new { id });
                }
            }

            // Схвалюємо
            var approvedStatus = await _context.Applicationstatuses.FirstOrDefaultAsync(s => s.Statusname == "Схвалено");
            app.Statusid = approvedStatus?.Statusid ?? 2;
            app.Decisiondate = DateTime.Now;

            // Знаходимо адміна в таблиці administrators
            var adminUser = await _userManager.GetUserAsync(User);
            var admin = await _context.Administrators.FirstOrDefaultAsync();
            if (admin != null) app.Adminid = admin.Adminid;

            _context.Update(app);
            await _context.SaveChangesAsync();

            // Пробуємо заселити або додаємо в чергу
            if (app.Applicationtype == "Поселення")
            {
                var alreadyInQueue = await _context.Queues.AnyAsync(q => q.Applicationid == app.Applicationid);
                var alreadySettled = await _context.Residencehistories
                    .AnyAsync(r => r.Studentid == app.Studentid && r.Checkoutdate == null);

                if (!alreadyInQueue && !alreadySettled)
                {
                    var assigned = await TryAssignRoom(app);
                    if (!assigned)
                        await RecalculateQueuePositions(app.Applicationid, student);
                }
            }

            TempData["Success"] = $"Заяву #{id} схвалено!";
            return RedirectToAction("Index");
        }

        // ============ ADMIN: відхилити ============
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "admin")]
        public async Task<IActionResult> Reject(int id, string rejectionreason)
        {
            var app = await _context.Applications.FindAsync(id);
            if (app == null) return NotFound();

            if (string.IsNullOrWhiteSpace(rejectionreason))
            {
                TempData["Error"] = "Вкажіть причину відмови!";
                return RedirectToAction("Review", new { id });
            }

            var rejectedStatus = await _context.Applicationstatuses.FirstOrDefaultAsync(s => s.Statusname == "Відхилено");
            app.Statusid = rejectedStatus?.Statusid ?? 3;
            app.Decisiondate = DateTime.Now;
            app.Rejectionreason = rejectionreason;

            var admin = await _context.Administrators.FirstOrDefaultAsync();
            if (admin != null) app.Adminid = admin.Adminid;

            _context.Update(app);
            await _context.SaveChangesAsync();

            // Видаляємо з черги якщо був
            var qe = await _context.Queues.FirstOrDefaultAsync(q => q.Applicationid == id);
            if (qe != null)
            {
                _context.Queues.Remove(qe);
                await _context.SaveChangesAsync();
                var rem = await _context.Queues.OrderBy(q => q.Position).ToListAsync();
                for (int i = 0; i < rem.Count; i++) rem[i].Position = i + 1;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Заяву #{id} відхилено.";
            return RedirectToAction("Index");
        }

        // ============ ADMIN: CRUD (Create, Edit, Delete) — залишаються як є ============

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
            ValidateDates(application);
            if (ModelState.IsValid)
            {
                try { _context.Update(application); await _context.SaveChangesAsync(); }
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
            var app = await _context.Applications.Include(a => a.Admin).Include(a => a.Status).Include(a => a.Student)
                .FirstOrDefaultAsync(m => m.Applicationid == id);
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

        [Authorize(Roles = "admin")] public IActionResult Import() => View();

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

        // ============ СТУДЕНТ: мої заяви ============
        [Authorize(Roles = "user")]
        public async Task<IActionResult> MyApplications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.StudentId == null) { ViewBag.Message = "Акаунт не прив'язаний до студента."; return View(new List<Application>()); }
            var apps = await _context.Applications.AsNoTracking().Include(a => a.Status)
                .Where(a => a.Studentid == user.StudentId.Value)
                .OrderByDescending(a => a.Submissiondate).ToListAsync();
            return View(apps);
        }

        // ============ СТУДЕНТ: подати заяву GET ============
        [Authorize(Roles = "user"), HttpGet]
        public async Task<IActionResult> StudentCreate()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.StudentId == null) { TempData["Error"] = "Акаунт не прив'язаний."; return RedirectToAction("MyApplications"); }
            var studentId = user.StudentId.Value;
            if (await _context.Applications.AnyAsync(a => a.Studentid == studentId && (a.Statusid == 1 || a.Statusid == 2)))
            { TempData["Error"] = "У вас вже є активна заява."; return RedirectToAction("MyApplications"); }

            var student = await _context.Students.FindAsync(studentId);
            var isSettled = await _context.Residencehistories.AnyAsync(r => r.Studentid == studentId && r.Checkoutdate == null);
            var uploadedDocs = await _context.Documents.AsNoTracking().Include(d => d.Type).Where(d => d.Studentid == studentId).ToListAsync();
            var requiredTypes = await _context.DocumentTypes.AsNoTracking()
                .Where(t => !t.IsPrivilegeDoc || (student != null && student.HasPrivilege)).ToListAsync();
            var uploadedTypeIds = uploadedDocs.Select(d => d.Typeid).ToHashSet();
            var missingTypes = requiredTypes.Where(t => !uploadedTypeIds.Contains(t.Typeid)).ToList();
            var mandatoryNames = new[] { "Паспорт", "ІПН", "Флюорографія", "Довідка з місця проживання" };
            var missingMandatory = requiredTypes.Where(t => mandatoryNames.Contains(t.Typename) && !uploadedTypeIds.Contains(t.Typeid)).Select(t => t.Typename).ToList();
            var expiredDocs = uploadedDocs.Where(d => d.Expirydate.HasValue && d.Expirydate.Value < DateTime.Today).Select(d => d.Type?.Typename ?? "").ToList();

            ViewBag.IsSettled = isSettled; ViewBag.Student = student; ViewBag.UploadedDocs = uploadedDocs;
            ViewBag.RequiredTypes = requiredTypes; ViewBag.MissingTypes = missingTypes;
            ViewBag.MissingMandatory = missingMandatory; ViewBag.ExpiredDocs = expiredDocs;
            ViewBag.HasPrivilege = student?.HasPrivilege ?? false;
            ViewBag.TypesJson = System.Text.Json.JsonSerializer.Serialize(missingTypes.Select(t => new { t.Typeid, t.RequiresIssueDate, t.IsLifetime, t.IsPrivilegeDoc, t.Typename }));
            return View();
        }

        // ============ СТУДЕНТ: завантажити документ inline ============
        [Authorize(Roles = "user"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocumentInline(int typeid, DateTime? issuedate, DateTime? expirydate, IFormFile uploadedFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.StudentId == null) return RedirectToAction("StudentCreate");
            var studentId = user.StudentId.Value;
            if (uploadedFile == null || uploadedFile.Length == 0) { TempData["Error"] = "Оберіть файл."; return RedirectToAction("StudentCreate"); }
            if (await _context.Documents.AnyAsync(d => d.Studentid == studentId && d.Typeid == typeid)) { TempData["Error"] = "Вже завантажено."; return RedirectToAction("StudentCreate"); }
            var docType = await _context.DocumentTypes.FindAsync(typeid);
            if (docType != null)
            {
                if (docType.RequiresIssueDate && !issuedate.HasValue) { TempData["Error"] = $"\"{docType.Typename}\" — вкажіть дату видачі."; return RedirectToAction("StudentCreate"); }
                if (!docType.IsLifetime && !expirydate.HasValue) { TempData["Error"] = $"\"{docType.Typename}\" — вкажіть дату закінчення."; return RedirectToAction("StudentCreate"); }
                if (issuedate.HasValue && issuedate > DateTime.Today) { TempData["Error"] = "Дата видачі в майбутньому."; return RedirectToAction("StudentCreate"); }
                if (issuedate.HasValue && expirydate.HasValue && expirydate <= issuedate) { TempData["Error"] = "Дата закінчення раніше видачі."; return RedirectToAction("StudentCreate"); }
                if (docType.Typename == "Флюорографія" && issuedate.HasValue && expirydate.HasValue && expirydate.Value.Date != issuedate.Value.AddYears(1).Date)
                { TempData["Error"] = $"Флюорографія — дата закінчення має бути {issuedate.Value.AddYears(1):dd.MM.yyyy}"; return RedirectToAction("StudentCreate"); }
            }
            using var ms = new MemoryStream(); await uploadedFile.CopyToAsync(ms);
            _context.Documents.Add(new Document { Studentid = studentId, Typeid = typeid, Filecontent = ms.ToArray(), Issuedate = issuedate, Expirydate = expirydate, Uploaddate = DateTime.Now });
            await _context.SaveChangesAsync();
            TempData["Success"] = $"\"{docType?.Typename}\" завантажено!";
            return RedirectToAction("StudentCreate");
        }

        // ============ СТУДЕНТ: подати заяву POST ============
        [Authorize(Roles = "user"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentCreate(string applicationtype, string academicperiod)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.StudentId == null) return RedirectToAction("MyApplications");
            var studentId = user.StudentId.Value;
            var student = await _context.Students.FindAsync(studentId);
            if (string.IsNullOrWhiteSpace(applicationtype)) { TempData["Error"] = "Оберіть тип."; return RedirectToAction("StudentCreate"); }
            if (string.IsNullOrWhiteSpace(academicperiod)) { TempData["Error"] = "Вкажіть період."; return RedirectToAction("StudentCreate"); }
            var isSettled = await _context.Residencehistories.AnyAsync(r => r.Studentid == studentId && r.Checkoutdate == null);
            if (applicationtype == "Пролонгація" && !isSettled) { TempData["Error"] = "Пролонгація — тільки для заселених."; return RedirectToAction("StudentCreate"); }
            if (applicationtype == "Поселення" && isSettled) { TempData["Error"] = "Ви вже заселені."; return RedirectToAction("StudentCreate"); }
            var uploadedTypeIds = await _context.Documents.Where(d => d.Studentid == studentId).Select(d => d.Typeid).ToListAsync();
            var mandatoryTypes = await _context.DocumentTypes.Where(t => new[] { "Паспорт", "ІПН", "Флюорографія", "Довідка з місця проживання" }.Contains(t.Typename)).ToListAsync();
            var missingM = mandatoryTypes.Where(t => !uploadedTypeIds.Contains(t.Typeid)).Select(t => t.Typename).ToList();
            if (missingM.Any()) { TempData["Error"] = $"Відсутні: {string.Join(", ", missingM)}"; return RedirectToAction("StudentCreate"); }
            var expiredD = await _context.Documents.Include(d => d.Type).Where(d => d.Studentid == studentId && d.Expirydate.HasValue && d.Expirydate.Value < DateTime.Today).Select(d => d.Type!.Typename).ToListAsync();
            if (expiredD.Any()) { TempData["Error"] = $"Прострочені: {string.Join(", ", expiredD)}"; return RedirectToAction("StudentCreate"); }
            if (student?.HasPrivilege == true && !await _context.Documents.Include(d => d.Type).AnyAsync(d => d.Studentid == studentId && d.Type!.IsPrivilegeDoc))
            { TempData["Error"] = "Завантажте пільговий документ."; return RedirectToAction("StudentCreate"); }

            var pending = await _context.Applicationstatuses.FirstOrDefaultAsync(s => s.Statusname == "На розгляді");
            _context.Applications.Add(new Application { Studentid = studentId, Statusid = pending?.Statusid ?? 1, Applicationtype = applicationtype, Submissiondate = DateTime.Now, Academicperiod = academicperiod });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Заяву подано!";
            return RedirectToAction("MyApplications");
        }

        // ============ Helpers ============
        private async Task LoadStudentDocuments(int? studentId)
        {
            if (studentId == null) { ViewBag.StudentDocuments = new List<Document>(); ViewBag.RequiredTypes = new List<DocumentType>(); return; }
            var student = await _context.Students.FindAsync(studentId);
            var docs = await _context.Documents.AsNoTracking().Include(d => d.Type).Where(d => d.Studentid == studentId).ToListAsync();
            var allTypes = await _context.DocumentTypes.AsNoTracking()
                .Where(t => !t.IsPrivilegeDoc || (student != null && student.HasPrivilege)).ToListAsync();
            var hasAnyPrivilegeDoc = docs.Any(d => allTypes.Any(at => at.Typeid == d.Typeid && at.IsPrivilegeDoc));
            var types = allTypes
                .Where(t => !t.IsPrivilegeDoc || !hasAnyPrivilegeDoc || docs.Any(d => d.Typeid == t.Typeid))
                .ToList();
            var uploadedTypeIds = docs.Select(d => d.Typeid).ToHashSet();
            var mandatoryNames = new[] { "Паспорт", "ІПН", "Флюорографія", "Довідка з місця проживання" };
            ViewBag.StudentDocuments = docs;
            ViewBag.RequiredTypes = types;
            ViewBag.MissingMandatory = types.Where(t => mandatoryNames.Contains(t.Typename) && !uploadedTypeIds.Contains(t.Typeid)).Select(t => t.Typename).ToList();
            ViewBag.ExpiredDocs = docs.Where(d => d.Expirydate.HasValue && d.Expirydate.Value < DateTime.Today).Select(d => d.Type?.Typename ?? "").ToList();
        }

        private async Task<bool> TryAssignRoom(Application app)
        {
            var student = await _context.Students.FindAsync(app.Studentid);
            if (student == null) return false;
            if (await _context.Residencehistories.AnyAsync(r => r.Studentid == app.Studentid && r.Checkoutdate == null)) return false;
            foreach (var room in await _context.Rooms.ToListAsync())
            {
                var cur = await _context.Residencehistories.Include(r => r.Student).Where(r => r.Roomid == room.Roomid && r.Checkoutdate == null).ToListAsync();
                if (cur.Count >= room.Capacity) continue;
                if (cur.Any() && cur.First().Student?.Gender != student.Gender) continue;
                _context.Residencehistories.Add(new Residencehistory { Studentid = app.Studentid, Roomid = room.Roomid, Checkindate = DateTime.Today });
                await _context.SaveChangesAsync(); return true;
            }
            return false;
        }

        private async Task RecalculateQueuePositions(int newAppId, Student? s)
        {
            if (s?.Studentid != null) s = await _context.Students.FindAsync(s.Studentid);
            var entries = await _context.Queues.Include(q => q.Application).ThenInclude(a => a!.Student).ToListAsync();
            entries.Add(new Queue { Applicationid = newAppId, Application = new Application { Applicationid = newAppId, Student = s } });
            var sorted = entries.OrderByDescending(q => q.Application?.Student?.HasPrivilege ?? false).ThenByDescending(q => q.Application?.Student?.DistanceKm ?? 0).ToList();
            _context.Queues.RemoveRange(await _context.Queues.ToListAsync());
            for (int i = 0; i < sorted.Count; i++) _context.Queues.Add(new Queue { Applicationid = sorted[i].Applicationid, Position = i + 1 });
            await _context.SaveChangesAsync();
        }

        private void ValidateDates(Application app)
        {
            if (app.Submissiondate.HasValue && app.Submissiondate > DateTime.Now) ModelState.AddModelError("Submissiondate", "В майбутньому");
            if (app.Decisiondate.HasValue && app.Submissiondate.HasValue && app.Decisiondate < app.Submissiondate) ModelState.AddModelError("Decisiondate", "Раніше подачі");
        }

        private bool ApplicationExists(int id) => _context.Applications.Any(e => e.Applicationid == id);
    }
}
