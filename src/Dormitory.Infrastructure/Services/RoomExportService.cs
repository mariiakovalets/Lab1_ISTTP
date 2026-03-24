using ClosedXML.Excel;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dormitory.Infrastructure.Services;

public class RoomExportService : IExportService<Room>
{
    private readonly DormitoryContext _context;

    private static readonly IReadOnlyList<string> Headers = new[]
    {
        "Номер кімнати", "Поверх", "Місткість", "Зайнято", "Вільно"
    };

    public RoomExportService(DormitoryContext context)
    {
        _context = context;
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanWrite)
            throw new ArgumentException("Потік не підтримує запис", nameof(stream));

        var rooms = await _context.Rooms
            .Include(r => r.Residencehistories)
            .OrderBy(r => r.Roomnumber)
            .ToListAsync(cancellationToken);

        var workbook  = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Кімнати");

        WriteHeader(worksheet);
        for (int i = 0; i < rooms.Count; i++)
            WriteRoom(worksheet, rooms[i], i + 2);

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(stream);
    }

    private static void WriteHeader(IXLWorksheet worksheet)
    {
        for (int i = 0; i < Headers.Count; i++)
            worksheet.Cell(1, i + 1).Value = Headers[i];
        worksheet.Row(1).Style.Font.Bold = true;
    }

    private static void WriteRoom(IXLWorksheet worksheet, Room room, int rowIndex)
    {
        var occupied = room.Residencehistories.Count(r => r.Checkoutdate == null);
        var free     = Math.Max(0, (room.Capacity ?? 0) - occupied);

        worksheet.Cell(rowIndex, 1).Value = room.Roomnumber;
        worksheet.Cell(rowIndex, 2).Value = room.Floor?.ToString() ?? "";
        worksheet.Cell(rowIndex, 3).Value = room.Capacity?.ToString() ?? "";
        worksheet.Cell(rowIndex, 4).Value = occupied;
        worksheet.Cell(rowIndex, 5).Value = free;
    }
}