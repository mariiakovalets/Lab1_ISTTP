using ClosedXML.Excel;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dormitory.Infrastructure.Services;

public class ApplicationImportService : IImportService<Application>
{
    private readonly DormitoryContext _context;

    public ApplicationImportService(DormitoryContext context)
    {
        _context = context;
    }

    public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Дані не можуть бути прочитані", nameof(stream));

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            try
            {
                await AddApplicationIfNotExistsAsync(row, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Пропускаємо дублікат — constraint в БД спрацював
                _context.ChangeTracker.Clear();
            }
        }
    }

    private async Task AddApplicationIfNotExistsAsync(IXLRow row, CancellationToken cancellationToken)
    {
        var applicationType = row.Cell(1).Value.ToString().Trim();
        var academicPeriod  = row.Cell(5).Value.ToString().Trim();
        var statusName      = row.Cell(6).Value.ToString().Trim();
        var studentName     = row.Cell(7).Value.ToString().Trim();

        if (string.IsNullOrWhiteSpace(studentName))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: ПІБ студента є обов'язковим");

        if (string.IsNullOrWhiteSpace(applicationType))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Тип заяви є обов'язковим");

        if (applicationType != "Поселення" && applicationType != "Пролонгація")
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Тип заяви має бути 'Поселення' або 'Пролонгація'");

        if (string.IsNullOrWhiteSpace(academicPeriod))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Академічний період є обов'язковим");

        if (string.IsNullOrWhiteSpace(statusName))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Статус є обов'язковим");

        // Валідація дат
        DateTime? submissionDate = ParseDate(row.Cell(2));
        DateTime? decisionDate   = ParseDate(row.Cell(3));

        if (submissionDate == null)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Дата подачі є обов'язковою (формат: рррр-мм-дд гг:хх)");

        if (submissionDate > DateTime.Now)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Дата подачі не може бути в майбутньому");

        if (submissionDate < new DateTime(2010, 1, 1))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Дата подачі не може бути раніше 2010 року");

        if (decisionDate.HasValue)
        {
            if (decisionDate > DateTime.Now)
                throw new InvalidOperationException($"Рядок {row.RowNumber()}: Дата рішення не може бути в майбутньому");

            if (decisionDate < submissionDate)
                throw new InvalidOperationException($"Рядок {row.RowNumber()}: Дата рішення не може бути раніше дати подачі");

            if (decisionDate > submissionDate.Value.AddDays(5))
                throw new InvalidOperationException($"Рядок {row.RowNumber()}: Дата рішення має бути не пізніше ніж через 5 днів від дати подачі");
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Fullname == studentName, cancellationToken);
        if (student == null)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Студента '{studentName}' не знайдено в БД");

        var status = await _context.Applicationstatuses
            .FirstOrDefaultAsync(s => s.Statusname == statusName, cancellationToken);
        if (status == null)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Статус '{statusName}' не знайдено в БД");

        // Перевіряємо чи вже є така заява (порівнюємо по хвилинах щоб уникнути різниці в секундах)
        var alreadyExists = await _context.Applications
            .AnyAsync(a => a.Studentid == student.Studentid
                && a.Applicationtype == applicationType
                && a.Academicperiod == academicPeriod
                && a.Submissiondate.HasValue
                && a.Submissiondate.Value.Date == submissionDate.Value.Date
                && a.Submissiondate.Value.Hour == submissionDate.Value.Hour
                && a.Submissiondate.Value.Minute == submissionDate.Value.Minute,
            cancellationToken);
        if (alreadyExists) return;

        var adminUsername = row.Cell(8).Value.ToString().Trim();
        var admin = await _context.Administrators
            .FirstOrDefaultAsync(a => a.Username == adminUsername, cancellationToken);

        _context.Applications.Add(new Application
        {
            Applicationtype = applicationType,
            Submissiondate  = submissionDate,
            Decisiondate    = decisionDate,
            Rejectionreason = row.Cell(4).Value.ToString().Trim(),
            Academicperiod  = academicPeriod,
            Statusid        = status.Statusid,
            Studentid       = student.Studentid,
            Adminid         = admin?.Adminid
        });
    }

    private static DateTime? ParseDate(IXLCell cell)
    {
        if (cell.Value.IsDateTime)
            return cell.GetDateTime();

        var str = cell.Value.ToString().Trim();
        if (string.IsNullOrWhiteSpace(str)) return null;

        return DateTime.TryParse(str, out var result) ? result : null;
    }
}
