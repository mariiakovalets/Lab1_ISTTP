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
    public class ResidencehistoryController : Controller
    {
        private readonly DormitoryContext _context;

        public ResidencehistoryController(DormitoryContext context)
        {
            _context = context;
        }

        // GET: Residencehistory
        public async Task<IActionResult> Index()
        {
            var dormitoryContext = _context.Residencehistories.Include(r => r.Room).Include(r => r.Student);
            return View(await dormitoryContext.ToListAsync());
        }

        // GET: Residencehistory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var residencehistory = await _context.Residencehistories
                .Include(r => r.Room)
                .Include(r => r.Student)
                .FirstOrDefaultAsync(m => m.Historyid == id);
            if (residencehistory == null)
            {
                return NotFound();
            }

            return View(residencehistory);
        }

        // GET: Residencehistory/Create
        public IActionResult Create()
        {
            ViewData["Roomid"] = new SelectList(_context.Rooms, "Roomid", "Roomnumber");
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname");
            return View();
        }

        // POST: Residencehistory/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Historyid,Studentid,Roomid,Checkindate,Checkoutdate")] Residencehistory residencehistory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(residencehistory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Roomid"] = new SelectList(_context.Rooms, "Roomid", "Roomnumber", residencehistory.Roomid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", residencehistory.Studentid);
            return View(residencehistory);
        }

        // GET: Residencehistory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var residencehistory = await _context.Residencehistories.FindAsync(id);
            if (residencehistory == null)
            {
                return NotFound();
            }
            ViewData["Roomid"] = new SelectList(_context.Rooms, "Roomid", "Roomnumber", residencehistory.Roomid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", residencehistory.Studentid);
            return View(residencehistory);
        }

        // POST: Residencehistory/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Historyid,Studentid,Roomid,Checkindate,Checkoutdate")] Residencehistory residencehistory)
        {
            if (id != residencehistory.Historyid)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(residencehistory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResidencehistoryExists(residencehistory.Historyid))
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
            ViewData["Roomid"] = new SelectList(_context.Rooms, "Roomid", "Roomnumber", residencehistory.Roomid);
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", residencehistory.Studentid);
            return View(residencehistory);
        }

        // GET: Residencehistory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var residencehistory = await _context.Residencehistories
                .Include(r => r.Room)
                .Include(r => r.Student)
                .FirstOrDefaultAsync(m => m.Historyid == id);
            if (residencehistory == null)
            {
                return NotFound();
            }

            return View(residencehistory);
        }

        // POST: Residencehistory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var residencehistory = await _context.Residencehistories.FindAsync(id);
            if (residencehistory != null)
            {
                _context.Residencehistories.Remove(residencehistory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ResidencehistoryExists(int id)
        {
            return _context.Residencehistories.Any(e => e.Historyid == id);
        }
    }
}
