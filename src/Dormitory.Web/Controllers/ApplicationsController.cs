using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
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

        // GET: Applications
        public async Task<IActionResult> Index()
        {
            var dormitoryContext = _context.Applications.Include(a => a.Admin).Include(a => a.Status).Include(a => a.Student);
            return View(await dormitoryContext.ToListAsync());
        }

        // GET: Applications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.Admin)
                .Include(a => a.Status)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(m => m.Applicationid == id);
            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // GET: Applications/Create
        public IActionResult Create()
        {
            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username");
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname");
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname");
            return View();
        }

        // POST: Applications/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Applicationid,Studentid,Statusid,Applicationtype,Submissiondate,Decisiondate,Rejectionreason,Extensionstartdate,Extensionenddate,Adminid,Academicperiod")] Application application)
        {
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

        // GET: Applications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound();
            }
            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username", application.Adminid);
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname", application.Statusid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", application.Studentid);
            return View(application);
        }

        // POST: Applications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Applicationid,Studentid,Statusid,Applicationtype,Submissiondate,Decisiondate,Rejectionreason,Extensionstartdate,Extensionenddate,Adminid,Academicperiod")] Application application)
        {
            if (id != application.Applicationid)
            {
                return NotFound();
            }

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
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Adminid"] = new SelectList(_context.Administrators, "Adminid", "Username", application.Adminid);
            ViewData["Statusid"] = new SelectList(_context.Applicationstatuses, "Statusid", "Statusname", application.Statusid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", application.Studentid);
            return View(application);
        }

        // GET: Applications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.Admin)
                .Include(a => a.Status)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(m => m.Applicationid == id);
            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // POST: Applications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application != null)
            {
                _context.Applications.Remove(application);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationExists(int id)
        {
            return _context.Applications.Any(e => e.Applicationid == id);
        }
    }
}
