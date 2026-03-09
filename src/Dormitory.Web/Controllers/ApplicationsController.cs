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
    public class ApplicationsController : Controller
    {
        private readonly DormitoryContext _context;

        public ApplicationsController(DormitoryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dormitoryContext = _context.Applications.Include(a => a.Admin).Include(a => a.Status).Include(a => a.Student);
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

            ValidateDates(application);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(application);
                    await _context.SaveChangesAsync();
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

        private void ValidateDates(Application application)
        {
// Дата рішення не може бути в майбутньому
if (application.Decisiondate.HasValue && application.Decisiondate > DateTime.Now)
    ModelState.AddModelError("Decisiondate", "Дата рішення не може бути в майбутньому");

// Дата рішення не пізніше ніж через 3 дні від дати подачі
if (application.Decisiondate.HasValue && application.Submissiondate.HasValue)
{
    if (application.Decisiondate < application.Submissiondate)
        ModelState.AddModelError("Decisiondate", "Дата рішення не може бути раніше дати подачі");
    
    if (application.Decisiondate > application.Submissiondate.Value.AddDays(5))
        ModelState.AddModelError("Decisiondate", "Дата рішення має бути не пізніше ніж через 5 днів від дати подачі");
}

            // Дата рішення не раніше дати подачі
            if (application.Decisiondate.HasValue && application.Submissiondate.HasValue)
            {
                if (application.Decisiondate < application.Submissiondate)
                    ModelState.AddModelError("Decisiondate", "Дата рішення не може бути раніше дати подачі");
            }

            // Дата початку продовження не пізніше дати кінця
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