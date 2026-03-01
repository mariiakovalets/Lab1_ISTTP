using System;
using System.Collections.Generic;

namespace Dormitory.Infrastructure;

public partial class Residencehistory
{
    public int Historyid { get; set; }

    public int? Studentid { get; set; }

    public int? Roomid { get; set; }

    public DateOnly? Checkindate { get; set; }

    public DateOnly? Checkoutdate { get; set; }

    public virtual Room? Room { get; set; }

    public virtual Student? Student { get; set; }
}
