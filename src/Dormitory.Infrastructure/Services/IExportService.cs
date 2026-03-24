namespace Dormitory.Infrastructure.Services;

public interface IExportService<TEntity> where TEntity : class
{
    Task WriteToAsync(Stream stream, CancellationToken cancellationToken);
}