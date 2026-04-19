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
    public class DocumentsController : Controller
    {
        private readonly DormitoryContext _context;

        public DocumentsController(DormitoryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dormitoryContext = _context.Documents.Include(d => d.Student).Include(d => d.Type);
            return View(await dormitoryContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents
                .Include(d => d.Student)
                .Include(d => d.Type)
                .FirstOrDefaultAsync(m => m.Documentid == id);

            if (document == null) return NotFound();
            return View(document);
        }

        public IActionResult Create()
        {
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname");
            ViewData["Typeid"] = new SelectList(_context.DocumentTypes, "Typeid", "Typename");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Documentid,Studentid,Typeid,Issuedate,Expirydate")] Document document, IFormFile uploadedFile)
        {
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await uploadedFile.CopyToAsync(memoryStream);
                document.Filecontent = memoryStream.ToArray();
            }

            document.Uploaddate = DateTime.Now;

            ModelState.Remove("Filecontent");
            ModelState.Remove("Uploaddate");

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                ModelState.AddModelError("Filecontent", "Завантажте скан-копію документа!");
            }

            var exists = await _context.Documents
                .AnyAsync(d => d.Studentid == document.Studentid && d.Typeid == document.Typeid);
            if (exists)
                ModelState.AddModelError("Typeid", "Цей студент вже має документ цього типу!");

            var docType = await _context.DocumentTypes.FindAsync(document.Typeid);
            if (docType != null)
            {
                if (docType.RequiresIssueDate == true && !document.Issuedate.HasValue)
                    ModelState.AddModelError("Issuedate", "Цей тип документа вимагає обов'язково вказати дату видачі!");

                if (docType.IsLifetime == false && !document.Expirydate.HasValue)
                    ModelState.AddModelError("Expirydate", "Цей документ не є довічним! Обов'язково вкажіть дату закінчення дії.");
            }

            ValidateDates(document, docType);

            if (ModelState.IsValid)
            {
                _context.Add(document);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", document.Studentid);
            ViewData["Typeid"] = new SelectList(_context.DocumentTypes, "Typeid", "Typename", document.Typeid);
            return View(document);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", document.Studentid);
            ViewData["Typeid"] = new SelectList(_context.DocumentTypes, "Typeid", "Typename", document.Typeid);
            return View(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Documentid,Studentid,Typeid,Issuedate,Expirydate")] Document document, IFormFile uploadedFile)
        {
            if (id != document.Documentid) return NotFound();

            ModelState.Remove("Filecontent");
            ModelState.Remove("Uploaddate");

            var docType = await _context.DocumentTypes.FindAsync(document.Typeid);
            if (docType != null)
            {
                if (docType.RequiresIssueDate == true && !document.Issuedate.HasValue)
                    ModelState.AddModelError("Issuedate", "Цей тип документа вимагає обов'язково вказати дату видачі!");

                if (docType.IsLifetime == false && !document.Expirydate.HasValue)
                    ModelState.AddModelError("Expirydate", "Цей документ не є довічним! Обов'язково вкажіть дату закінчення дії.");
            }

            ValidateDates(document, docType);

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDoc = await _context.Documents.FindAsync(id);
                    if (existingDoc == null) return NotFound();

                    existingDoc.Studentid = document.Studentid;
                    existingDoc.Typeid = document.Typeid;
                    existingDoc.Issuedate = document.Issuedate;
                    existingDoc.Expirydate = document.Expirydate;

                    if (uploadedFile != null && uploadedFile.Length > 0)
                    {
                        using var memoryStream = new MemoryStream();
                        await uploadedFile.CopyToAsync(memoryStream);
                        existingDoc.Filecontent = memoryStream.ToArray();
                    }

                    _context.Update(existingDoc);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentExists(document.Documentid)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", document.Studentid);
            ViewData["Typeid"] = new SelectList(_context.DocumentTypes, "Typeid", "Typename", document.Typeid);
            return View(document);
        }

        private void ValidateDates(Document document, DocumentType? docType)
        {
            if (document.Issuedate.HasValue && document.Issuedate > DateTime.Today)
                ModelState.AddModelError("Issuedate", "Дата видачі не може бути в майбутньому");

            if (document.Issuedate.HasValue && document.Issuedate < new DateTime(2010, 1, 1))
                ModelState.AddModelError("Issuedate", "Дата видачі не може бути раніше 2010 року");

            if (document.Issuedate.HasValue && document.Expirydate.HasValue)
            {
                if (document.Expirydate <= document.Issuedate)
                    ModelState.AddModelError("Expirydate", "Дата закінчення має бути пізніше дати видачі");

                if (docType != null && docType.Typename == "Флюорографія")
                {
                    var expectedExpiry = document.Issuedate.Value.AddYears(1);
                    if (document.Expirydate.Value.Date != expectedExpiry.Date)
                        ModelState.AddModelError("Expirydate", $"Флюорографія дійсна рівно 1 рік — дата закінчення має бути {expectedExpiry:dd.MM.yyyy}");
                }
            }
        }

        public async Task<IActionResult> Download(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents
                .Include(d => d.Type)
                .FirstOrDefaultAsync(d => d.Documentid == id);

            if (document == null || document.Filecontent == null || document.Filecontent.Length == 0)
                return NotFound();

            string contentType = "application/octet-stream";
            string extension = "bin";

            if (document.Filecontent.Length > 3)
            {
                if (document.Filecontent[0] == 0x25 && document.Filecontent[1] == 0x50)
                { contentType = "application/pdf"; extension = "pdf"; }
                else if (document.Filecontent[0] == 0xFF && document.Filecontent[1] == 0xD8)
                { contentType = "image/jpeg"; extension = "jpg"; }
                else if (document.Filecontent[0] == 0x89 && document.Filecontent[1] == 0x50)
                { contentType = "image/png"; extension = "png"; }
            }

            string typeName = document.Type?.Typename ?? "Document";
            string fileName = $"{typeName}_{id}.{extension}";
            return File(document.Filecontent, contentType, fileName);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents
                .Include(d => d.Student)
                .Include(d => d.Type)
                .FirstOrDefaultAsync(m => m.Documentid == id);

            if (document == null) return NotFound();
            return View(document);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document != null)
                _context.Documents.Remove(document);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.Documentid == id);
        }
    }
}