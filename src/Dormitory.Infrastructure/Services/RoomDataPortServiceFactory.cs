using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;

namespace Dormitory.Infrastructure.Services;

public class RoomDataPortServiceFactory : IDataPortServiceFactory<Room>
{
    private readonly DormitoryContext _context;

    public RoomDataPortServiceFactory(DormitoryContext context)
    {
        _context = context;
    }

    public IImportService<Room> GetImportService(string contentType)
    {
        if (contentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            return new RoomImportService(_context);

        throw new NotImplementedException($"Імпорт не підтримується для типу {contentType}");
    }

    public IExportService<Room> GetExportService(string contentType)
    {
        if (contentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            return new RoomExportService(_context);

        throw new NotImplementedException($"Експорт не підтримується для типу {contentType}");
    }
}