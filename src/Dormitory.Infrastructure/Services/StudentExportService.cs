using ClosedXML.Excel;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dormitory.Infrastructure.Services;

public class StudentExportService : IExportService<Student>
{
    private readonly DormitoryContext _context;

    private static readonly IReadOnlyList<string> Headers = new[]
    {
        "ПІБ", "Курс", "Дата народження", "Адреса",
        "Відстань (км)", "Телефон", "Email", "Стать",
        "Факультет", "Пільга"
    };

    public StudentExportService(DormitoryContext context)
    {
        _context = context;
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanWrite)
            throw new ArgumentException("Потік не підтримує запис", nameof(stream));

        var students = await _context.Students
            .Include(s => s.Faculty)
            .OrderBy(s => s.Fullname)
            .ToListAsync(cancellationToken);

        var workbook  = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Студенти");

        WriteHeader(worksheet);

        for (int i = 0; i < students.Count; i++)
            WriteStudent(worksheet, students[i], i + 2);

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(stream);
    }

    private static void WriteHeader(IXLWorksheet worksheet)
    {
        for (int i = 0; i < Headers.Count; i++)
            worksheet.Cell(1, i + 1).Value = Headers[i];

        worksheet.Row(1).Style.Font.Bold = true;
    }

    private static void WriteStudent(IXLWorksheet worksheet, Student student, int rowIndex)
    {
        worksheet.Cell(rowIndex, 1).Value = student.Fullname;
        worksheet.Cell(rowIndex, 2).Value = student.Course?.ToString() ?? "";
        worksheet.Cell(rowIndex, 3).Value = student.Birthdate?.ToString("yyyy-MM-dd") ?? "";
        worksheet.Cell(rowIndex, 4).Value = student.Address ?? "";
        worksheet.Cell(rowIndex, 5).Value = student.DistanceKm?.ToString() ?? "";
        worksheet.Cell(rowIndex, 6).Value = student.Phone ?? "";
        worksheet.Cell(rowIndex, 7).Value = student.Email ?? "";
        worksheet.Cell(rowIndex, 8).Value = student.Gender ?? "";
        worksheet.Cell(rowIndex, 9).Value = student.Faculty?.Facultyname ?? "";
        worksheet.Cell(rowIndex, 10).Value = student.HasPrivilege == true ? "Так" : "Ні";
    }
}