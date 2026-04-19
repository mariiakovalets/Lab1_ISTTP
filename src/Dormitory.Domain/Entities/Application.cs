using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; 

namespace Dormitory.Domain.Entities;


public partial class Application : IValidatableObject
{
    [Display(Name = "Номер заявки")]
    public int Applicationid { get; set; }

    [Display(Name = "Студент")]
    [Required(ErrorMessage = "Будь ласка, оберіть студента!")]
    public int? Studentid { get; set; }

    [Display(Name = "Статус заявки")]
    public int? Statusid { get; set; }

    [Display(Name = "Тип заявки")]
    [Required(ErrorMessage = "Вкажіть тип (наприклад, Поселення або Пролонгація)")]
    public string? Applicationtype { get; set; }

    [Display(Name = "Дата подачі")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? Submissiondate { get; set; }

    [Display(Name = "Дата рішення")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? Decisiondate { get; set; }

    [Display(Name = "Причина відмови")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public string? Rejectionreason { get; set; }


    [Display(Name = "Початок пролонгації")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = false)]
    public DateOnly? Extensionstartdate { get; set; }

    [Display(Name = "Кінець пролонгації")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = false)]
    public DateOnly? Extensionenddate { get; set; }

    [Display(Name = "Адміністратор")]
    public int? Adminid { get; set; }

    [Display(Name = "Академічний період")]
    [Required(ErrorMessage = "Вкажіть період (наприклад, 2025/2026-1)")]
    public string? Academicperiod { get; set; }

    [Display(Name = "Адмін")]
    public virtual Administrator? Admin { get; set; }

    public virtual Queue? Queue { get; set; }

    [Display(Name = "Статус")]
    public virtual Applicationstatus? Status { get; set; }

    [Display(Name = "Студент")]
    public virtual Student? Student { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Statusid == 3 && string.IsNullOrWhiteSpace(Rejectionreason))
        {
            yield return new ValidationResult(
                "Якщо заявку відхилено, ви ЗОБОВ'ЯЗАНІ вказати причину відмови!",
                new[] { nameof(Rejectionreason) }
            );
        }

        if (Applicationtype == "Пролонгація")
        {
            if (!Extensionstartdate.HasValue || !Extensionenddate.HasValue)
            {
                yield return new ValidationResult(
                    "Для заяви на пролонгацію необхідно обов'язково вказати дати початку та кінця!",
                    new[] { nameof(Extensionstartdate), nameof(Extensionenddate) } 
                );
            }
            else if (Extensionstartdate > Extensionenddate)
            {
                yield return new ValidationResult(
                    "Дата початку пролонгації не може бути пізнішою за дату кінця!",
                    new[] { nameof(Extensionstartdate), nameof(Extensionenddate) }
                );
            }
        }
    }
}