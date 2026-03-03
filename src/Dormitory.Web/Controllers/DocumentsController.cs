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
    public class DocumentsController : Controller
    {
        private readonly DormitoryContext _context;

        public DocumentsController(DormitoryContext context)
        {
            _context = context;
        }

        // GET: Documents
        public async Task<IActionResult> Index()
        {
            var dormitoryContext = _context.Documents.Include(d => d.Student).Include(d => d.Type);
            return View(await dormitoryContext.ToListAsync());
        }

        // GET: Documents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(d => d.Student)
                .Include(d => d.Type)
                .FirstOrDefaultAsync(m => m.Documentid == id);
            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }

        // GET: Documents/Create
        public IActionResult Create()
        {
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname");
            ViewData["Typeid"] = new SelectList(_context.DocumentTypes, "Typeid", "Typename");
            return View();
        }

        // POST: Documents/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create([Bind("Documentid,Studentid,Typeid,Issuedate,Expirydate,Uploaddate")] Document document, IFormFile uploadedFile)
{
    // --- 1. Логіка збереження файлу ---
    if (uploadedFile != null && uploadedFile.Length > 0)
    {
        using (var memoryStream = new MemoryStream())
        {
            await uploadedFile.CopyToAsync(memoryStream);
            document.Filecontent = memoryStream.ToArray();
        }
    }
    else
    {
        document.Filecontent = new byte[0];
    }
    ModelState.Remove("Filecontent");

    // --- 2. РОЗУМНА ВАЛІДАЦІЯ ---
    // УВАГА: якщо _context.Document_types підкреслює червоним, зміни на _context.DocumentTypes
    var docType = await _context.DocumentTypes.FindAsync(document.Typeid); 
    
    if (docType != null)
    {
        if (docType.RequiresIssueDate == true && !document.Issuedate.HasValue)
        {
            ModelState.AddModelError("Issuedate", "Цей тип документа вимагає обов'язково вказати дату видачі!");
        }

        if (docType.IsLifetime == false && !document.Expirydate.HasValue)
        {
            ModelState.AddModelError("Expirydate", "Цей документ не є довічним! Обов'язково вкажіть дату закінчення дії.");
        }
    }

    // --- 3. Збереження ---
    if (ModelState.IsValid)
    {
        _context.Add(document);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    
    // --- 4. ОСЬ ЦЕЙ ШМАТОК БУВ ВТРАЧЕНИЙ ---
    // Якщо є помилка, повертаємо користувача на форму і заново малюємо випадаючі списки
    ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", document.Studentid);
    
    // Знову ж таки, якщо _context.Document_types світиться червоним, зміни на DocumentTypes
    ViewData["Typeid"] = new SelectList(_context.DocumentTypes, "Typeid", "Typename", document.Typeid);
    
    return View(document); // Повертаємо сторінку з червоним текстом помилок
}
    

        // GET: Documents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }
            ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", document.Studentid);
            ViewData["Typeid"] = new SelectList(_context.DocumentTypes, "Typeid", "Typename", document.Typeid);
            return View(document);
        }

        // POST: Documents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, [Bind("Documentid,Studentid,Typeid,Issuedate,Expirydate,Uploaddate")] Document document, IFormFile uploadedFile)
{
    if (id != document.Documentid)
    {
        return NotFound();
    }

    // --- 1. РОЗУМНА ВАЛІДАЦІЯ ДАТ (як і при створенні) ---
    var docType = await _context.DocumentTypes.FindAsync(document.Typeid); 
    if (docType != null)
    {
        if (docType.RequiresIssueDate == true && !document.Issuedate.HasValue)
        {
            ModelState.AddModelError("Issuedate", "Цей тип документа вимагає обов'язково вказати дату видачі!");
        }
        if (docType.IsLifetime == false && !document.Expirydate.HasValue)
        {
            ModelState.AddModelError("Expirydate", "Цей документ не є довічним! Обов'язково вкажіть дату закінчення дії.");
        }
    }

    ModelState.Remove("Filecontent"); // Просимо не сваритися на файл

    if (ModelState.IsValid)
    {
        try
        {
            // --- 2. ЛОГІКА ЗБЕРЕЖЕННЯ ---
            // Знаходимо старий документ у базі даних
            var existingDoc = await _context.Documents.FindAsync(id);
            if (existingDoc == null) return NotFound();

            // Оновлюємо звичайні дані
            existingDoc.Studentid = document.Studentid;
            existingDoc.Typeid = document.Typeid;
            existingDoc.Issuedate = document.Issuedate;
            existingDoc.Expirydate = document.Expirydate;
            existingDoc.Uploaddate = document.Uploaddate;

            // Якщо користувач завантажив НОВИЙ файл - перезаписуємо
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await uploadedFile.CopyToAsync(memoryStream);
                    existingDoc.Filecontent = memoryStream.ToArray();
                }
            }
            // Якщо новий файл не завантажили - existingDoc.Filecontent просто залишиться старим!

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
    
    // Якщо є помилки, повертаємо на сторінку
    ViewData["Studentid"] = new SelectList(_context.Students, "Studentid", "Fullname", document.Studentid);
    ViewData["Typeid"] = new SelectList(_context.DocumentTypes, "Typeid", "Typename", document.Typeid);
    return View(document);
}

        // GET: Documents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(d => d.Student)
                .Include(d => d.Type)
                .FirstOrDefaultAsync(m => m.Documentid == id);
            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document != null)
            {
                _context.Documents.Remove(document);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.Documentid == id);
        }
    }
}
