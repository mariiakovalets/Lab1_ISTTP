using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dormitory.Domain.Entities;

public partial class Student
{
    public int Studentid { get; set; }

    [Display(Name = "ПІБ студента")]
    [Required(ErrorMessage = "Введіть ПІБ студента")]
    public string Fullname { get; set; } = null!;

    [Display(Name = "Курс")]
    [Required(ErrorMessage = "Оберіть курс")]
    public int? Course { get; set; }

    [Display(Name = "Дата народження")]
    [Required(ErrorMessage = "Введіть дату народження")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = false)]
    public DateOnly? Birthdate { get; set; }

    [Display(Name = "Адреса")]
    [Required(ErrorMessage = "Введіть адресу")]
    public string? Address { get; set; }

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress(ErrorMessage = "Введіть коректний email")]
    public string? Email { get; set; }

    [Display(Name = "Стать")]
    [Required(ErrorMessage = "Оберіть стать")]
    public string? Gender { get; set; }

    [Display(Name = "Факультет")]
    [Required(ErrorMessage = "Оберіть факультет")]
    public int? Facultyid { get; set; }

    [Display(Name = "Телефон")]
    [Required(ErrorMessage = "Введіть телефон")]
    public string? Phone { get; set; }

    [Display(Name = "Відстань (км)")]
    [Required(ErrorMessage = "Введіть відстань до Києва")]
    [Range(1, int.MaxValue, ErrorMessage = "Відстань має бути більше 0 км")]
    public int? DistanceKm { get; set; }

    public virtual Application? Application { get; set; }
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual Faculty? Faculty { get; set; }
    public virtual ICollection<Residencehistory> Residencehistories { get; set; } = new List<Residencehistory>();
}