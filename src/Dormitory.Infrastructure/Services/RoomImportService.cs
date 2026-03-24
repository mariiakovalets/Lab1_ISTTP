using ClosedXML.Excel;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dormitory.Infrastructure.Services;

public class RoomImportService : IImportService<Room>
{
    private readonly DormitoryContext _context;

    public RoomImportService(DormitoryContext context)
    {
        _context = context;
    }

public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
{
    if (!stream.CanRead)
        throw new ArgumentException("Дані не можуть бути прочитані", nameof(stream));

    using var workbook = new XLWorkbook(stream);
    var worksheet = workbook.Worksheets.First();

    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    try
    {
        foreach (var row in worksheet.RowsUsed().Skip(1))
            await AddOrUpdateRoomAsync(row, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}

    private async Task AddOrUpdateRoomAsync(IXLRow row, CancellationToken cancellationToken)
    {
        var roomNumber = row.Cell(1).Value.ToString().Trim();
        if (string.IsNullOrWhiteSpace(roomNumber)) return;

        var existing = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Roomnumber == roomNumber, cancellationToken);

        if (existing != null)
        {
            // Кімната вже є — оновлюємо поверх і місткість
            existing.Floor    = GetFloor(row);
            existing.Capacity = GetCapacity(row);
        }
        else
        {
            // Нова кімната — додаємо
            _context.Rooms.Add(new Room
            {
                Roomnumber = roomNumber,
                Floor      = GetFloor(row),
                Capacity   = GetCapacity(row)
            });
        }
    }

    private static int? GetFloor(IXLRow row)
    {
        var val = row.Cell(2).Value.ToString();
        return int.TryParse(val, out var result) ? result : null;
    }

    private static int? GetCapacity(IXLRow row)
    {
        var val = row.Cell(3).Value.ToString();
        return int.TryParse(val, out var result) ? result : null;
    }
}