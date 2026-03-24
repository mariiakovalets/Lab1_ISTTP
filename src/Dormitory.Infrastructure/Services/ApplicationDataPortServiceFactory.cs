using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;

namespace Dormitory.Infrastructure.Services;

public class ApplicationDataPortServiceFactory : IDataPortServiceFactory<Application>
{
    private readonly DormitoryContext _context;

    public ApplicationDataPortServiceFactory(DormitoryContext context)
    {
        _context = context;
    }

    public IImportService<Application> GetImportService(string contentType)
    {
        if (contentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            return new ApplicationImportService(_context);

        throw new NotImplementedException($"Імпорт не підтримується для типу {contentType}");
    }

    public IExportService<Application> GetExportService(string contentType)
    {
        if (contentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            return new ApplicationExportService(_context);

        throw new NotImplementedException($"Експорт не підтримується для типу {contentType}");
    }
}