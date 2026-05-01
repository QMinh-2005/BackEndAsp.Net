using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class OrderStatus
{
    public int OrderStatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
