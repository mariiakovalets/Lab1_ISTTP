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

namespace Dormitory.Web.Controllers
{
    [Authorize(Roles = "admin")]
    public class FacultiesController : Controller
    {
        private readonly DormitoryContext _context;

        public FacultiesController(DormitoryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Faculties.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties
                .FirstOrDefaultAsync(m => m.Facultyid == id);
            if (faculty == null) return NotFound();

            return View(faculty);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Facultyid,Facultyname")] Faculty faculty)
        {
            if (ModelState.IsValid)
            {
                _context.Add(faculty);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(faculty);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound();

            return View(faculty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Facultyid,Facultyname")] Faculty faculty)
        {
            if (id != faculty.Facultyid) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(faculty);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FacultyExists(faculty.Facultyid))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(faculty);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties
                .FirstOrDefaultAsync(m => m.Facultyid == id);
            if (faculty == null) return NotFound();

            return View(faculty);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty != null)
                _context.Faculties.Remove(faculty);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FacultyExists(int id)
        {
            return _context.Faculties.Any(e => e.Facultyid == id);
        }
    }
}