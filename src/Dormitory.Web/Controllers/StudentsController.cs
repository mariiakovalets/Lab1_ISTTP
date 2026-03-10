using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;

namespace Dormitory.Web.Controllers
{
    public class StudentsController : Controller
    {
        private readonly DormitoryContext _context;

        public StudentsController(DormitoryContext context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            var dormitoryContext = _context.Students.Include(s => s.Faculty);
            return View(await dormitoryContext.ToListAsync());
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.Studentid == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            ViewData["Facultyid"] = new SelectList(_context.Faculties, "Facultyid", "Facultyname");
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Studentid,Fullname,Course,Birthdate,Address,Email,Gender,Facultyid,Phone,DistanceKm,HasPrivilege")] Student student)
        {
            // Нормалізуємо телефон перед валідацією
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

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            ViewData["Facultyid"] = new SelectList(_context.Faculties, "Facultyid", "Facultyname", student.Facultyid);
            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Studentid,Fullname,Course,Birthdate,Address,Email,Gender,Facultyid,Phone,DistanceKm,HasPrivilege")] Student student)
        {
            if (id != student.Studentid) return NotFound();

            // Нормалізуємо телефон перед валідацією
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

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.Studentid == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Studentid == id);
        }
    }
}