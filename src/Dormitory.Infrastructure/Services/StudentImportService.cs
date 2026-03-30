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
        var fullname = GetFullname(row);
        if (string.IsNullOrWhiteSpace(fullname))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: ПІБ є обов'язковим");

        var email = GetEmail(row);
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Email є обов'язковим");
        if (!email.Contains('@'))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Некоректний email '{email}'");

        var course = GetCourse(row);
        if (course == null)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Курс є обов'язковим і має бути числом");
        if (course < 1 || course > 6)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Курс має бути від 1 до 6");

        var birthdate = GetBirthdate(row);
        if (birthdate == null)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Дата народження є обов'язковою (формат: рррр-мм-дд)");
        if (birthdate < new DateOnly(1995, 1, 1) || birthdate > new DateOnly(2009, 12, 31))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Дата народження має бути між 1995 і 2009 роком");

        var address = GetAddress(row);
        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Адреса є обов'язковою");

        var distanceKm = GetDistanceKm(row);
        if (distanceKm == null)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Відстань є обов'язковою і має бути числом");
        if (distanceKm <= 0)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Відстань має бути більше 0");

        var phone = GetPhone(row);
if (string.IsNullOrWhiteSpace(phone))
    throw new InvalidOperationException($"Рядок {row.RowNumber()}: Телефон є обов'язковим");

var phoneClean = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
if (!System.Text.RegularExpressions.Regex.IsMatch(phoneClean, @"^(\+380|0)\d{9}$"))
    throw new InvalidOperationException($"Рядок {row.RowNumber()}: Телефон '{phone}' має бути у форматі 0XXXXXXXXX або +380XXXXXXXXX");

phone = phoneClean;

        var gender = GetGender(row);
        if (gender == null)
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Стать має бути 'Ч' або 'Ж'");

        var facultyName = GetFacultyName(row);
        if (string.IsNullOrWhiteSpace(facultyName))
            throw new InvalidOperationException($"Рядок {row.RowNumber()}: Факультет є обов'язковим");

        var faculty = await _context.Faculties
            .FirstOrDefaultAsync(f => f.Facultyname == facultyName, cancellationToken);

        if (faculty == null)
        {
            faculty = new Faculty { Facultyname = facultyName };
            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var existing = await _context.Students
            .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

        if (existing != null)
        {
            existing.Fullname     = fullname;
            existing.Course       = course;
            existing.Birthdate    = birthdate;
            existing.Address      = address;
            existing.DistanceKm   = distanceKm;
            existing.Phone        = phone;
            existing.Gender       = gender;
            existing.HasPrivilege = GetHasPrivilege(row);
            existing.Facultyid    = faculty.Facultyid;
        }
        else
        {
            _context.Students.Add(new Student
            {
                Fullname     = fullname,
                Course       = course,
                Birthdate    = birthdate,
                Address      = address,
                DistanceKm   = distanceKm,
                Phone        = phone,
                Email        = email,
                Gender       = gender,
                HasPrivilege = GetHasPrivilege(row),
                Facultyid    = faculty.Facultyid
            });
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