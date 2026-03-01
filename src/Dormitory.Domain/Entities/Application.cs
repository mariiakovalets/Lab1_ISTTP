using System;
using System.Collections.Generic;

namespace Dormitory.Infrastructure;

public partial class Application
{
    public int Applicationid { get; set; }

    public int? Studentid { get; set; }

    public int? Statusid { get; set; }

    public string? Applicationtype { get; set; }

    public DateTime? Submissiondate { get; set; }

    public DateTime? Decisiondate { get; set; }

    public string? Rejectionreason { get; set; }

    public DateOnly? Extensionstartdate { get; set; }

    public DateOnly? Extensionenddate { get; set; }

    public int? Adminid { get; set; }

    public string? Academicperiod { get; set; }

    public virtual Administrator? Admin { get; set; }

    public virtual Queue? Queue { get; set; }

    public virtual Applicationstatus? Status { get; set; }

    public virtual Student? Student { get; set; }
}
