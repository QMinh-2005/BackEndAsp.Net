using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Status { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Payment? Payment { get; set; }

    public virtual ICollection<ServiceTicket> ServiceTickets { get; set; } = new List<ServiceTicket>();

    public virtual User User { get; set; } = null!;
}
