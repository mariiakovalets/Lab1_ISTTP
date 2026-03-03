using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dormitory.Domain.Entities;

public partial class Faculty
{
    public int Facultyid { get; set; }

    [Display(Name = "Назва факультету")]
    [Required(ErrorMessage = "Введіть назву факультету")]
    public string Facultyname { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
