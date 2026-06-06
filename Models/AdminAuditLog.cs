using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class AdminAuditLog
{
    public int AuditLogId { get; set; }

    public int? AdminId { get; set; }

    public string? AdminEmail { get; set; }

    public string Module { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string TargetType { get; set; } = null!;

    public int? TargetId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? Admin { get; set; }
}
