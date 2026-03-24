namespace Dormitory.Infrastructure.Services;

public interface IImportService<TEntity> where TEntity : class
{
    Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken);
}