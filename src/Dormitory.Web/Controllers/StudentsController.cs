using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Dormitory.Infrastructure.Services;

namespace Dormitory.Web.Controllers
{
    [Authorize(Roles = "admin")]
    public class StudentsController : Controller
    {
        private readonly DormitoryContext _context;
        private readonly StudentDataPortServiceFactory _studentDataPortServiceFactory;

        public StudentsController(DormitoryContext context)
        {
            _context = context;
            _studentDataPortServiceFactory = new StudentDataPortServiceFactory(context);
        }

        public async Task<IActionResult> Index()
        {
            var dormitoryContext = _context.Students.Include(s => s.Faculty);
            return View(await dormitoryContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.Studentid == id);

            if (student == null) return NotFound();

            return View(student);
        }

        public IActionResult Create()
        {
            ViewData["Facultyid"] = new SelectList(_context.Faculties, "Facultyid", "Facultyname");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Studentid,Fullname,Course,Birthdate,Address,Email,Gender,Facultyid,Phone,DistanceKm,HasPrivilege")] Student student)
        {
            if (!string.IsNullOrEmpty(student.Phone))
            {
                student.Phone = student.Phone
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("(", "")
                    .Replace(")", "");

                ModelState.Remove("Phone");

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    student.Phone, @"^(\+380|0)\d{9}$"))
                {
                    ModelState.AddModelError("Phone",
                        "Введіть номер у форматі 0XXXXXXXXX або +380XXXXXXXXX");
                }
            }

            if (student.Birthdate < new DateOnly(1995, 1, 1) || student.Birthdate > new DateOnly(2009, 12, 31))
            {
                ModelState.AddModelError("Birthdate", "Дата народження має бути між 1995 і 2009 роком");
            }

            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["Facultyid"] = new SelectList(_context.Faculties, "Facultyid", "Facultyname", student.Facultyid);
            return View(student);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            ViewData["Facultyid"] = new SelectList(_context.Faculties, "Facultyid", "Facultyname", student.Facultyid);
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Studentid,Fullname,Course,Birthdate,Address,Email,Gender,Facultyid,Phone,DistanceKm,HasPrivilege")] Student student)
        {
            if (id != student.Studentid) return NotFound();

            if (!string.IsNullOrEmpty(student.Phone))
            {
                student.Phone = student.Phone
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("(", "")
                    .Replace(")", "");

                ModelState.Remove("Phone");

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    student.Phone, @"^(\+380|0)\d{9}$"))
                {
                    ModelState.AddModelError("Phone",
                        "Введіть номер у форматі 0XXXXXXXXX або +380XXXXXXXXX");
                }
            }

            if (student.Birthdate < new DateOnly(1995, 1, 1) || student.Birthdate > new DateOnly(2009, 12, 31))
            {
                ModelState.AddModelError("Birthdate", "Дата народження має бути між 1995 і 2009 роком");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Studentid))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["Facultyid"] = new SelectList(_context.Faculties, "Facultyid", "Facultyname", student.Facultyid);
            return View(student);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.Studentid == id);

            if (student == null) return NotFound();

            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
                _context.Students.Remove(student);

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
            ModelState.Remove("fileExcel");

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
                var importService = _studentDataPortServiceFactory.GetImportService(fileExcel.ContentType);
                using var stream = fileExcel.OpenReadStream();
                await importService.ImportFromStreamAsync(stream, cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Помилка при імпорті. Перевірте, що файл відповідає очікуваному формату.");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var exportService = _studentDataPortServiceFactory.GetExportService(contentType);

            var memoryStream = new MemoryStream();
            await exportService.WriteToAsync(memoryStream, cancellationToken);
            await memoryStream.FlushAsync(cancellationToken);
            memoryStream.Position = 0;

            return new FileStreamResult(memoryStream, contentType)
            {
                FileDownloadName = "students_export.xlsx"
            };
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Studentid == id);
        }
    }
}