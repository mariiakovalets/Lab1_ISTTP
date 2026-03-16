using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dormitory.Infrastructure.Data;

namespace Dormitory.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChartsController : ControllerBase
{
    private readonly DormitoryContext _context;

    public ChartsController(DormitoryContext context)
    {
        _context = context;
    }

    // Діаграма 1: кількість студентів по факультетах
    private record StudentsByFacultyItem(string Faculty, int Count);

    [HttpGet("studentsByFaculty")]
    public async Task<JsonResult> GetStudentsByFacultyAsync(CancellationToken cancellationToken)
    {
        var data = await _context.Students
            .Include(s => s.Faculty)
            .GroupBy(s => s.Faculty!.Facultyname)
            .Select(g => new StudentsByFacultyItem(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        return new JsonResult(data);
    }

    // Діаграма 2: статуси заяв
    private record ApplicationsByStatusItem(string Status, int Count);

    [HttpGet("applicationsByStatus")]
    public async Task<JsonResult> GetApplicationsByStatusAsync(CancellationToken cancellationToken)
    {
        var data = await _context.Applications
            .Include(a => a.Status)
            .GroupBy(a => a.Status!.Statusname)
            .Select(g => new ApplicationsByStatusItem(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        return new JsonResult(data);
    }

// Діаграма 3: студенти по курсах
private record StudentsByCourseItem(string Course, int Count);

[HttpGet("studentsByCourse")]
public async Task<JsonResult> GetStudentsByCourseAsync(CancellationToken cancellationToken)
{
    var data = await _context.Students
        .Where(s => s.Course != null)
        .GroupBy(s => s.Course!.Value)
        .Select(g => new { Course = g.Key, Count = g.Count() })
        .OrderBy(x => x.Course)
        .ToListAsync(cancellationToken);

    var result = data.Select(x => new StudentsByCourseItem(
        x.Course + " курс",
        x.Count
    )).ToList();

    return new JsonResult(result);
}

// Діаграма 4: кількість документів по типах
private record DocumentsByTypeItem(string DocumentType, int Count);

[HttpGet("documentsByType")]
public async Task<JsonResult> GetDocumentsByTypeAsync(CancellationToken cancellationToken)
{
    var data = await _context.Documents
        .Include(d => d.Type)
        .GroupBy(d => d.Type!.Typename)
        .Select(g => new DocumentsByTypeItem(g.Key, g.Count()))
        .ToListAsync(cancellationToken);

    return new JsonResult(data);
}

 // Діаграма 5: заселеність кімнат
private record RoomOccupancyItem(string Room, int Occupied, int Free);

[HttpGet("roomOccupancy")]
public async Task<JsonResult> GetRoomOccupancyAsync(CancellationToken cancellationToken)
{
    var rooms = await _context.Rooms.ToListAsync(cancellationToken);
    var activeResidents = await _context.Residencehistories
        .Where(r => r.Checkoutdate == null)
        .GroupBy(r => r.Roomid)
        .Select(g => new { RoomId = g.Key, Count = g.Count() })
        .ToListAsync(cancellationToken);

    var result = rooms.Select(r => {
        var occupied = activeResidents.FirstOrDefault(x => x.RoomId == r.Roomid)?.Count ?? 0;
        return new RoomOccupancyItem(
            "Кімн. " + r.Roomnumber,
            occupied,
            Math.Max(0, (r.Capacity ?? 0) - occupied)
        );
    }).ToList();

    return new JsonResult(result);
}
}

