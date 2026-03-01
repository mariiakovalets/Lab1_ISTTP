using System;
using System.Collections.Generic;

namespace Dormitory.Infrastructure;

public partial class Applicationstatus
{
    public int Statusid { get; set; }

    public string Statusname { get; set; } = null!;

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}
