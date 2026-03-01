using System;
using System.Collections.Generic;

namespace Dormitory.Infrastructure;

public partial class Administrator
{
    public int Adminid { get; set; }

    public string Username { get; set; } = null!;

    public string? Fullname { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}
