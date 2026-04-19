using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;

namespace Dormitory.Web.Controllers
{
    [Authorize]
    public class QueuesController : Controller
    {
        private readonly DormitoryContext _context;
        private readonly UserManager<User> _userManager;

        public QueuesController(DormitoryContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ADMIN: повна черга
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var queue = await _context.Queues
                .Include(q => q.Application)
                    .ThenInclude(a => a!.Student)
                .Include(q => q.Application)
                    .ThenInclude(a => a!.Status)
                .OrderBy(q => q.Position)
                .ToListAsync();

            return View(queue);
        }

        // СТУДЕНТ: моя позиція
        [Authorize(Roles = "user")]
        public async Task<IActionResult> MyPosition()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.StudentId == null)
            {
                ViewBag.Message = "Ваш акаунт не прив'язаний до жодного студента.";
                ViewBag.Position = null;
                return View();
            }

            var myEntry = await _context.Queues
                .Include(q => q.Application)
                    .ThenInclude(a => a!.Student)
                .Where(q => q.Application!.Studentid == user.StudentId)
                .FirstOrDefaultAsync();

            var totalInQueue = await _context.Queues.CountAsync();

            if (myEntry != null)
            {
                ViewBag.Position = myEntry.Position;
                ViewBag.Total = totalInQueue;
                ViewBag.Message = null;
            }
            else
            {
                // Перевіряємо — може студент вже заселений?
                var isSettled = await _context.Residencehistories
                    .Include(r => r.Room)
                    .Where(r => r.Studentid == user.StudentId && r.Checkoutdate == null)
                    .FirstOrDefaultAsync();

                if (isSettled != null)
                {
                    ViewBag.Message = $"Ви вже заселені в кімнату {isSettled.Room?.Roomnumber}.";
                }
                else
                {
                    ViewBag.Message = "Ви не перебуваєте в черзі на поселення.";
                }
                ViewBag.Position = null;
            }

            return View();
        }
    }
}
