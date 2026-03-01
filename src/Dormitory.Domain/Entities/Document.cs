using System;
using System.Collections.Generic;

namespace Dormitory.Infrastructure;

public partial class Document
{
    public int Documentid { get; set; }

    public int Studentid { get; set; }

    public int Typeid { get; set; }

    public byte[] Filecontent { get; set; } = null!;

    public DateOnly? Issuedate { get; set; }

    public DateOnly? Expirydate { get; set; }

    public DateTime? Uploaddate { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual DocumentType Type { get; set; } = null!;
}
