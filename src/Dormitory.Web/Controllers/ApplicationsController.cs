using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Dormitory.Infrastructure.Services;

namespace Dormitory.Web.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly DormitoryContext _context;
        private readonly ApplicationDataPortServiceFactory _applicationDataPortServiceFactory;

        public ApplicationsController(DormitoryContext context)
        {
            _context = context;
            _applicationDataPortServiceFactory = new ApplicationDataPortServiceFactory(context);
        }

        public async Task<IActionResult> Index()
        {
            var dormitoryContext = _context.Applications
                .AsNoTracking()
                .Include(a => a.Admin)
                .Include(a => a.Status)
                .Include(a => a.Student)
                .OrderByDescending(a => a.Submissiondate);
            return View(await dormitoryContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var application = await _context.Applications
                .Include(a => a.Admin)
                .Include(a => a.Status)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(m => m.Applicationid == id);

            if (application == null) return NotFound();
            return View(application);
        }

        public IActionResult Create()
        {
            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username");
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname");
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Applicationid,Studentid,Statusid,Applicationtype,Submissiondate,Decisiondate,Rejectionreason,Extensionstartdate,Extensionenddate,Adminid,Academicperiod")] Application application)
        {
            // Перевірка чи студент вже заселений
            if (application.Applicationtype == "Поселення")
            {
                var alreadySettled = await _context.Residencehistories
                    .AnyAsync(r => r.Studentid == application.Studentid && r.Checkoutdate == null);
                if (alreadySettled)
                    ModelState.AddModelError("Studentid", "Цей студент вже заселений!");
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

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var application = await _context.Applications.FindAsync(id);
            if (application == null) return NotFound();

            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username", application.Adminid);
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname", application.Statusid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", application.Studentid);
            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Applicationid,Studentid,Statusid,Applicationtype,Submissiondate,Decisiondate,Rejectionreason,Extensionstartdate,Extensionenddate,Adminid,Academicperiod")] Application application)
        {
            if (id != application.Applicationid) return NotFound();

            // Перевірка чи студент вже має активну заяву на поселення (крім поточної)
            if (application.Applicationtype == "Поселення")
            {
                var alreadySettled = await _context.Residencehistories
                    .AnyAsync(r => r.Studentid == application.Studentid && r.Checkoutdate == null);
                if (alreadySettled)
                    ModelState.AddModelError("Studentid", "Цей студент вже заселений!");

                var duplicateApplication = await _context.Applications
                    .AnyAsync(a => a.Studentid == application.Studentid
                                && a.Applicationtype == "Поселення"
                                && a.Applicationid != application.Applicationid
                                && a.Decisiondate == null);
                if (duplicateApplication)
                    ModelState.AddModelError("Studentid", "Цей студент вже має активну заяву на поселення!");
            }

            ValidateDates(application);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(application);
                    await _context.SaveChangesAsync();

                    // Якщо заяву відхилено — видаляємо з черги
                    if (application.Statusid == 3)
                    {
                        var queueEntry = await _context.Queues
                            .FirstOrDefaultAsync(q => q.Applicationid == application.Applicationid);
                        if (queueEntry != null)
                        {
                            _context.Queues.Remove(queueEntry);
                            await _context.SaveChangesAsync();

                            // Перераховуємо позиції
                            var remaining = await _context.Queues.OrderBy(q => q.Position).ToListAsync();
                            for (int i = 0; i < remaining.Count; i++)
                                remaining[i].Position = i + 1;
                            await _context.SaveChangesAsync();
                        }
                    }

                    // Логіка при схваленні заяви на поселення
                    if (application.Statusid == 2 && application.Applicationtype == "Поселення")
                    {
                        var alreadyInQueue = await _context.Queues
                            .AnyAsync(q => q.Applicationid == application.Applicationid);
                        var alreadySettled = await _context.Residencehistories
                            .AnyAsync(r => r.Studentid == application.Studentid && r.Checkoutdate == null);

                        if (!alreadyInQueue && !alreadySettled)
                        {
                            var assigned = await TryAssignRoom(application);
                            if (!assigned)
                            {
                                var student = await _context.Students.FindAsync(application.Studentid);
                                await RecalculateQueuePositions(application.Applicationid, student);
                            }
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationExists(application.Applicationid))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username", application.Adminid);
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname", application.Statusid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", application.Studentid);
            return View(application);
        }

        private async Task<bool> TryAssignRoom(Application application)
        {
            var student = await _context.Students.FindAsync(application.Studentid);
            if (student == null) return false;

            var rooms = await _context.Rooms.ToListAsync();

            foreach (var room in rooms)
            {
                var currentResidents = await _context.Residencehistories
                    .Include(r => r.Student)
                    .Where(r => r.Roomid == room.Roomid && r.Checkoutdate == null)
                    .ToListAsync();

                var occupiedSpaces = currentResidents.Count;

                if (occupiedSpaces >= room.Capacity) continue;

                if (currentResidents.Any())
                {
                    var roomGender = currentResidents.First().Student?.Gender;
                    if (roomGender != student.Gender) continue;
                }

                // Заселяємо!
                _context.Residencehistories.Add(new Residencehistory
                {
                    Studentid = application.Studentid,
                    Roomid = room.Roomid,
                    Checkindate = DateTime.Today
                });
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        private async Task RecalculateQueuePositions(int newApplicationId, Student? newStudent)
        {
            // Завантажуємо студента свіжо з БД щоб мати всі поля
            if (newStudent?.Studentid != null)
                newStudent = await _context.Students.FindAsync(newStudent.Studentid);

            var queueEntries = await _context.Queues
                .Include(q => q.Application)
                    .ThenInclude(a => a!.Student)
                .ToListAsync();

            queueEntries.Add(new Queue
            {
                Applicationid = newApplicationId,
                Application = new Application
                {
                    Applicationid = newApplicationId,
                    Student = newStudent
                }
            });

            var sorted = queueEntries
                .OrderByDescending(q => q.Application?.Student?.HasPrivilege ?? false)
                .ThenByDescending(q => q.Application?.Student?.DistanceKm ?? 0)
                .ToList();

            _context.Queues.RemoveRange(await _context.Queues.ToListAsync());

            for (int i = 0; i < sorted.Count; i++)
            {
                _context.Queues.Add(new Queue
                {
                    Applicationid = sorted[i].Applicationid,
                    Position = i + 1
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var application = await _context.Applications
                .Include(a => a.Admin)
                .Include(a => a.Status)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(m => m.Applicationid == id);

            if (application == null) return NotFound();
            return View(application);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application != null)
                _context.Applications.Remove(application);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Import()
        {
            return View();
        }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Import(IFormFile fileExcel, CancellationToken cancellationToken)
{
    if (fileExcel == null || fileExcel.Length == 0)
    {
        ModelState.AddModelError("", "Оберіть файл для завантаження");
        return View();
    }

    const string xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    if (fileExcel.ContentType != xlsx)
    {
        ModelState.AddModelError("", "Підтримується лише формат .xlsx");
        return View();
    }

    try
    {
        var importService = _applicationDataPortServiceFactory.GetImportService(fileExcel.ContentType);
        using var stream = fileExcel.OpenReadStream();
        await importService.ImportFromStreamAsync(stream, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
    catch (Exception)
    {
        ModelState.AddModelError("", "Помилка при імпорті. Перевірте що файл відповідає очікуваному формату.");
        return View();
    }
}

        [HttpGet]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var exportService = _applicationDataPortServiceFactory.GetExportService(contentType);

            var memoryStream = new MemoryStream();
            await exportService.WriteToAsync(memoryStream, cancellationToken);
            await memoryStream.FlushAsync(cancellationToken);
            memoryStream.Position = 0;

            return new FileStreamResult(memoryStream, contentType)
            {
                FileDownloadName = $"applications_{DateTime.UtcNow:yyyy-MM-dd}.xlsx"
            };
        }

        private void ValidateDates(Application application)
        {
            if (application.Submissiondate.HasValue && application.Submissiondate > DateTime.Now)
                ModelState.AddModelError("Submissiondate", "Дата подачі не може бути в майбутньому");

            if (application.Submissiondate.HasValue && application.Submissiondate < new DateTime(2010, 1, 1))
                ModelState.AddModelError("Submissiondate", "Дата подачі не може бути раніше 2010 року");

            if (application.Decisiondate.HasValue && application.Decisiondate > DateTime.Now)
                ModelState.AddModelError("Decisiondate", "Дата рішення не може бути в майбутньому");

            if (application.Decisiondate.HasValue && application.Submissiondate.HasValue)
            {
                if (application.Decisiondate < application.Submissiondate)
                    ModelState.AddModelError("Decisiondate", "Дата рішення не може бути раніше дати подачі");

                if (application.Decisiondate > application.Submissiondate.Value.AddDays(5))
                    ModelState.AddModelError("Decisiondate", "Дата рішення має бути не пізніше ніж через 5 днів від дати подачі");
            }

            if (application.Extensionstartdate.HasValue && application.Extensionenddate.HasValue)
            {
                if (application.Extensionenddate < application.Extensionstartdate)
                    ModelState.AddModelError("Extensionenddate", "Дата кінця продовження не може бути раніше дати початку");
            }
        }

        private bool ApplicationExists(int id)
        {
            return _context.Applications.Any(e => e.Applicationid == id);
        }
    }
}