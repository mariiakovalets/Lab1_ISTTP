using System;
using System.Collections.Generic;

namespace Dormitory.Infrastructure;

public partial class Faculty
{
    public int Facultyid { get; set; }

    public string Facultyname { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
