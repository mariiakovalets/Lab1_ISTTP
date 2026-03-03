using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dormitory.Domain.Entities; 

public partial class Residencehistory
{
    public int Historyid { get; set; }

    [Display(Name = "Студент")]
    public int? Studentid { get; set; }

    [Display(Name = "Кімната")]
    public int? Roomid { get; set; }

    [Display(Name = "Дата заселення")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
    public DateTime? Checkindate { get; set; }

    [Display(Name = "Дата виселення")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
    public DateTime? Checkoutdate { get; set; }

    [Display(Name = "Кімната")]
    public virtual Room? Room { get; set; }

    [Display(Name = "Студент")]
    public virtual Student? Student { get; set; }
}