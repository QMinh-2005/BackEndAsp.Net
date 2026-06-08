using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class ReturnRequest
{
    public int ReturnRequestId { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    public string MainReason { get; set; } = null!;

    public string DetailReason { get; set; } = null!;

    public string? CustomerDescription { get; set; }

    public string Status { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? AdminNote { get; set; }

    public decimal? RefundAmount { get; set; }

    public int? OriginalOrderStatusId { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ReturnRequestImage> ReturnRequestImages { get; set; } = new List<ReturnRequestImage>();

    public virtual User User { get; set; } = null!;
}
