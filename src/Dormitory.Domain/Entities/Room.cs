using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dormitory.Domain.Entities;

public partial class Room
{
    public int Roomid { get; set; }

    [Display(Name = "Номер кімнати")]
    [Required(ErrorMessage = "Введіть номер кімнати")]
    public string Roomnumber { get; set; } = null!;

    [Display(Name = "Поверх")]
    [Required(ErrorMessage = "Введіть поверх")]
    [Range(1, 20, ErrorMessage = "Поверх має бути від 1 до 20")]
    public int? Floor { get; set; }

    [Display(Name = "Місткість")]
    [Required(ErrorMessage = "Введіть місткість кімнати")]
    [Range(2, 3, ErrorMessage = "Місткість має бути від 2 до 3")]
    public int? Capacity { get; set; }

    public virtual ICollection<Residencehistory> Residencehistories { get; set; } = new List<Residencehistory>();
}
