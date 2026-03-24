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

    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    try
    {
        foreach (var row in worksheet.RowsUsed().Skip(1))
            await AddApplicationIfNotExistsAsync(row, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}

    private async Task AddApplicationIfNotExistsAsync(IXLRow row, CancellationToken cancellationToken)
    {
        var studentName    = row.Cell(7).Value.ToString().Trim();
        var applicationType = row.Cell(1).Value.ToString().Trim();
        var academicPeriod  = row.Cell(5).Value.ToString().Trim();

        if (string.IsNullOrWhiteSpace(studentName)) return;

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Fullname == studentName, cancellationToken);
        if (student == null) return;

        var statusName = row.Cell(6).Value.ToString().Trim();
        var status = await _context.Applicationstatuses
            .FirstOrDefaultAsync(s => s.Statusname == statusName, cancellationToken);
        if (status == null) return;

        // Аналіз: перевіряємо чи вже є активна заява для цього студента
        // на цей тип і академічний період
        var alreadyExists = await _context.Applications
            .AnyAsync(a => a.Studentid == student.Studentid
                        && a.Applicationtype == applicationType
                        && a.Academicperiod == academicPeriod
                        && (a.Statusid == 1 || a.Statusid == 2),
                cancellationToken);

        if (alreadyExists) return;

        var adminUsername = row.Cell(8).Value.ToString().Trim();
        var admin = await _context.Administrators
            .FirstOrDefaultAsync(a => a.Username == adminUsername, cancellationToken);

        var application = new Application
        {
            Applicationtype = applicationType,
            Submissiondate  = DateTime.TryParse(row.Cell(2).Value.ToString(), out var sub) ? sub : DateTime.Now,
            Decisiondate    = DateTime.TryParse(row.Cell(3).Value.ToString(), out var dec) ? dec : null,
            Rejectionreason = row.Cell(4).Value.ToString().Trim(),
            Academicperiod  = academicPeriod,
            Statusid        = status.Statusid,
            Studentid       = student.Studentid,
            Adminid         = admin?.Adminid
        };

        _context.Applications.Add(application);
    }
}