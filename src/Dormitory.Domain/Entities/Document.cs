using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dormitory.Domain.Entities;

public partial class Document
{
    public int Documentid { get; set; }

    [Display(Name = "Студент")]
    [Required(ErrorMessage = "Оберіть студента!")]
    public int Studentid { get; set; }

    [Display(Name = "Тип документа")]
    [Required(ErrorMessage = "Оберіть тип документа!")]
    public int Typeid { get; set; }

    [Display(Name = "Скан-копія")]
    public byte[]? Filecontent { get; set; }

    [Display(Name = "Дата видачі")]
    [DataType(DataType.Date)] 
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
    public DateTime? Issuedate { get; set; }

    [Display(Name = "Дата закінчення дії")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
    public DateTime? Expirydate { get; set; }

    [Display(Name = "Дата завантаження")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
    public DateTime? Uploaddate { get; set; }

    public virtual Student? Student { get; set; }
    public virtual DocumentType? Type { get; set; } 
}