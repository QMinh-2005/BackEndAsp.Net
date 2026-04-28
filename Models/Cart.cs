using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Cart
{
    public int CartId { get; set; }

    public int? DetailId { get; set; }

    public int? Quantity { get; set; }

    public DateTime? AddedDate { get; set; }

    public int UserId { get; set; }

    public virtual ProductDetail? Detail { get; set; }

    public virtual User User { get; set; } = null!;
}
