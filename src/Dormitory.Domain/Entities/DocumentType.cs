using System;
using System.Collections.Generic;

namespace Dormitory.Infrastructure;

public partial class DocumentType
{
    public int Typeid { get; set; }

    public string Typename { get; set; } = null!;

    public bool RequiresIssueDate { get; set; }

    public bool IsLifetime { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
