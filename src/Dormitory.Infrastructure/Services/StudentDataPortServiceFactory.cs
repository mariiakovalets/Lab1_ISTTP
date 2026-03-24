using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;

namespace Dormitory.Infrastructure.Services;

public class StudentDataPortServiceFactory : IDataPortServiceFactory<Student>
{
    private readonly DormitoryContext _context;

    public StudentDataPortServiceFactory(DormitoryContext context)
    {
        _context = context;
    }

    public IImportService<Student> GetImportService(string contentType)
    {
        if (contentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            return new StudentImportService(_context);

        throw new NotImplementedException($"Імпорт не підтримується для типу {contentType}");
    }

    public IExportService<Student> GetExportService(string contentType)
    {
        if (contentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            return new StudentExportService(_context);

        throw new NotImplementedException($"Експорт не підтримується для типу {contentType}");
    }
}