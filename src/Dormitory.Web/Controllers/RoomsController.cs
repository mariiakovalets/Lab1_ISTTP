using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Dormitory.Infrastructure.Services;

namespace Dormitory.Web.Controllers
{
    [Authorize(Roles = "admin,superadmin")]
    public class RoomsController : Controller
    {
        private readonly DormitoryContext _context;
        private readonly RoomDataPortServiceFactory _roomDataPortServiceFactory;

        public RoomsController(DormitoryContext context)
        {
            _context = context;
            _roomDataPortServiceFactory = new RoomDataPortServiceFactory(context);
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Rooms.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.Roomid == id);
            if (room == null) return NotFound();
            return View(room);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Roomid,Roomnumber,Floor,Capacity")] Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Roomid,Roomnumber,Floor,Capacity")] Room room)
        {
            if (id != room.Roomid) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.Roomid)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.Roomid == id);
            if (room == null) return NotFound();
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null) _context.Rooms.Remove(room);
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
                var importService = _roomDataPortServiceFactory.GetImportService(fileExcel.ContentType);
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
            var exportService = _roomDataPortServiceFactory.GetExportService(contentType);
            var memoryStream = new MemoryStream();
            await exportService.WriteToAsync(memoryStream, cancellationToken);
            await memoryStream.FlushAsync(cancellationToken);
            memoryStream.Position = 0;
            return new FileStreamResult(memoryStream, contentType)
            {
                FileDownloadName = "rooms_export.xlsx"
            };
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Roomid == id);
        }
    }
}