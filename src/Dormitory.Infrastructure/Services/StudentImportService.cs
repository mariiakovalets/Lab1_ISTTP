using ClosedXML.Excel;
using Dormitory.Domain.Entities;
using Dormitory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dormitory.Infrastructure.Services;

public class StudentImportService : IImportService<Student>
{
    private readonly DormitoryContext _context;

    public StudentImportService(DormitoryContext context)
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
            await AddOrUpdateStudentAsync(row, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}

    private async Task AddOrUpdateStudentAsync(IXLRow row, CancellationToken cancellationToken)
    {
        var email = GetEmail(row);
        if (string.IsNullOrWhiteSpace(email)) return;

        var facultyName = GetFacultyName(row);
        var faculty = await _context.Faculties
            .FirstOrDefaultAsync(f => f.Facultyname == facultyName, cancellationToken);

        if (faculty == null && !string.IsNullOrWhiteSpace(facultyName))
        {
            faculty = new Faculty { Facultyname = facultyName };
            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var existing = await _context.Students
            .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

        if (existing != null)
        {
            // Студент вже є — оновлюємо його дані
            existing.Fullname    = GetFullname(row);
            existing.Course      = GetCourse(row);
            existing.Birthdate   = GetBirthdate(row);
            existing.Address     = GetAddress(row);
            existing.DistanceKm  = GetDistanceKm(row);
            existing.Phone       = GetPhone(row);
            existing.Gender      = GetGender(row);
            existing.HasPrivilege = GetHasPrivilege(row);
            existing.Facultyid   = faculty?.Facultyid;
        }
        else
        {
            // Новий студент — додаємо
            var student = new Student
            {
                Fullname     = GetFullname(row),
                Course       = GetCourse(row),
                Birthdate    = GetBirthdate(row),
                Address      = GetAddress(row),
                DistanceKm   = GetDistanceKm(row),
                Phone        = GetPhone(row),
                Email        = email,
                Gender       = GetGender(row),
                HasPrivilege = GetHasPrivilege(row),
                Facultyid    = faculty?.Facultyid
            };
            _context.Students.Add(student);
        }
    }

    private static string GetFullname(IXLRow row) =>
        row.Cell(1).Value.ToString().Trim();

    private static int? GetCourse(IXLRow row)
    {
        var val = row.Cell(2).Value.ToString();
        return int.TryParse(val, out var result) ? result : null;
    }

    private static DateOnly? GetBirthdate(IXLRow row)
    {
        var val = row.Cell(3).Value.ToString();
        return DateOnly.TryParse(val, out var result) ? result : null;
    }

    private static string? GetAddress(IXLRow row) =>
        row.Cell(4).Value.ToString().Trim();

    private static int? GetDistanceKm(IXLRow row)
    {
        var val = row.Cell(5).Value.ToString();
        return int.TryParse(val, out var result) ? result : null;
    }

    private static string? GetPhone(IXLRow row) =>
        row.Cell(6).Value.ToString().Trim();

    private static string? GetEmail(IXLRow row) =>
        row.Cell(7).Value.ToString().Trim();

    private static string? GetGender(IXLRow row)
    {
        var val = row.Cell(8).Value.ToString().Trim();
        return val == "Ч" || val == "Ж" ? val : null;
    }

    private static string GetFacultyName(IXLRow row) =>
        row.Cell(9).Value.ToString().Trim();

    private static bool GetHasPrivilege(IXLRow row)
    {
        var val = row.Cell(10).Value.ToString().Trim().ToLower();
        return val is "так" or "true" or "1";
    }
}