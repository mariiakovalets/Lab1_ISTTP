using ClosedXML.Excel;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dormitory.Infrastructure.Services;

public class ApplicationExportService : IExportService<Application>
{
    private readonly DormitoryContext _context;

    private static readonly IReadOnlyList<string> Headers = new[]
    {
        "Тип заяви", "Дата подачі", "Дата рішення",
        "Причина відмови", "Академічний період",
        "Статус", "Студент", "Адмін"
    };

    public ApplicationExportService(DormitoryContext context)
    {
        _context = context;
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanWrite)
            throw new ArgumentException("Потік не підтримує запис", nameof(stream));

        var applications = await _context.Applications
            .Include(a => a.Student)
            .Include(a => a.Status)
            .Include(a => a.Admin)
            .OrderByDescending(a => a.Submissiondate)
            .ToListAsync(cancellationToken);

        var workbook  = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Заяви");

        WriteHeader(worksheet);
        for (int i = 0; i < applications.Count; i++)
            WriteApplication(worksheet, applications[i], i + 2);

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(stream);
    }

    private static void WriteHeader(IXLWorksheet worksheet)
    {
        for (int i = 0; i < Headers.Count; i++)
            worksheet.Cell(1, i + 1).Value = Headers[i];
        worksheet.Row(1).Style.Font.Bold = true;
    }

    private static void WriteApplication(IXLWorksheet worksheet, Application app, int rowIndex)
    {
        worksheet.Cell(rowIndex, 1).Value = app.Applicationtype ?? "";
        worksheet.Cell(rowIndex, 2).Value = app.Submissiondate?.ToString("yyyy-MM-dd HH:mm") ?? "";
        worksheet.Cell(rowIndex, 3).Value = app.Decisiondate?.ToString("yyyy-MM-dd HH:mm") ?? "";
        worksheet.Cell(rowIndex, 4).Value = app.Rejectionreason ?? "";
        worksheet.Cell(rowIndex, 5).Value = app.Academicperiod ?? "";
        worksheet.Cell(rowIndex, 6).Value = app.Status?.Statusname ?? "";
        worksheet.Cell(rowIndex, 7).Value = app.Student?.Fullname ?? "";
        worksheet.Cell(rowIndex, 8).Value = app.Admin?.Username ?? "";
    }
}