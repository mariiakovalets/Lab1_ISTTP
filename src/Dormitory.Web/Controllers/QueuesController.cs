using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;

namespace Dormitory.Web.Controllers
{
    public class QueuesController : Controller
    {
        private readonly DormitoryContext _context;

        public QueuesController(DormitoryContext context)
        {
            _context = context;
        }

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
    }
}