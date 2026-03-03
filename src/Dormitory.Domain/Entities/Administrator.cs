using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dormitory.Domain.Entities;

public partial class Administrator
{
    public int Adminid { get; set; }

    [Display(Name = "Логін")]
    [Required(ErrorMessage = "Введіть логін")]
    public string Username { get; set; } = null!;

    [Display(Name = "ПІБ")]
    [Required(ErrorMessage = "Введіть ПІБ адміністратора")]
    public string? Fullname { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}
