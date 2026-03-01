using System;
using System.Collections.Generic;

namespace Dormitory.Infrastructure;

public partial class Room
{
    public int Roomid { get; set; }

    public string Roomnumber { get; set; } = null!;

    public int? Floor { get; set; }

    public int? Capacity { get; set; }

    public virtual ICollection<Residencehistory> Residencehistories { get; set; } = new List<Residencehistory>();
}
