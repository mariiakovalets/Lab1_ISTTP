using System.ComponentModel.DataAnnotations;

namespace Dormitory.Web.Models;

public class RegisterViewModel
{
    // --- Дані акаунту ---

    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress(ErrorMessage = "Некоректний email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Введіть пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Підтвердіть пароль")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Паролі не співпадають")]
    [Display(Name = "Підтвердження пароля")]
    public string PasswordConfirm { get; set; } = null!;

    // --- Персональні дані студента ---

    [Required(ErrorMessage = "Введіть ПІБ")]
    [Display(Name = "Повне ім'я (ПІБ)")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Оберіть курс")]
    [Display(Name = "Курс")]
    [Range(1, 6, ErrorMessage = "Курс має бути від 1 до 6")]
    public int? Course { get; set; }

    [Required(ErrorMessage = "Введіть дату народження")]
    [Display(Name = "Дата народження")]
    [DataType(DataType.Date)]
    public DateOnly? Birthdate { get; set; }

    [Required(ErrorMessage = "Введіть адресу")]
    [Display(Name = "Адреса (місто проживання)")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Введіть телефон")]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Оберіть стать")]
    [Display(Name = "Стать")]
    public string? Gender { get; set; }

    [Required(ErrorMessage = "Оберіть факультет")]
    [Display(Name = "Факультет")]
    public int? FacultyId { get; set; }

    [Required(ErrorMessage = "Введіть відстань до Києва")]
    [Display(Name = "Відстань до Києва (км)")]
    [Range(1, 10000, ErrorMessage = "Відстань має бути більше 0")]
    public int? DistanceKm { get; set; }

    [Display(Name = "Наявність пільги")]
    public bool HasPrivilege { get; set; }
}
