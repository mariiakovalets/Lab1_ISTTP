using System;
using System.Collections.Generic;

namespace Dormitory.Domain.Entities;

public partial class Queue
{
    public int Queueid { get; set; }

    public int? Applicationid { get; set; }

    public int? Position { get; set; }

    public virtual Application? Application { get; set; }
}
