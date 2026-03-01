using System;
using System.Collections.Generic;

namespace Dormitory.Infrastructure;

public partial class Student
{
    public int Studentid { get; set; }

    public string Fullname { get; set; } = null!;

    public int? Course { get; set; }

    public DateOnly? Birthdate { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Gender { get; set; }

    public int? Facultyid { get; set; }

    public virtual Application? Application { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Faculty? Faculty { get; set; }

    public virtual ICollection<Residencehistory> Residencehistories { get; set; } = new List<Residencehistory>();
}
