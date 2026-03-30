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

        if (string.IsNullOrWhiteSpace(roomNumber))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Номер кімнати є обов'язковим");

        if (roomNumber.Length > 20)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Номер кімнати не може бути довшим за 20 символів");

        var floor = GetFloor(row);
        if (floor == null)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Поверх є обов'язковим і має бути числом");
        if (floor < 1 || floor > 30)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Поверх має бути від 1 до 30");

        var capacity = GetCapacity(row);
        if (capacity == null)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Місткість є обов'язковою і має бути числом");
        if (capacity < 2 || capacity > 3)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Місткість має бути від 2 до 3");

        var existing = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Roomnumber == roomNumber, cancellationToken);

        if (existing != null)
        {
            existing.Floor    = floor;
            existing.Capacity = capacity;
        }
        else
        {
            _context.Rooms.Add(new Room
            {
                Roomnumber = roomNumber,
                Floor      = floor,
                Capacity   = capacity
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