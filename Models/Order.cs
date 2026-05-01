using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public int UserId { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Note { get; set; }

    public string? ReceiverName { get; set; }

    public decimal? ShippingFee { get; set; }

    public int? OrderStatusId { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual OrderStatus? OrderStatus { get; set; }

    public virtual Payment? Payment { get; set; }

    public virtual ICollection<ServiceTicket> ServiceTickets { get; set; } = new List<ServiceTicket>();

    public virtual User User { get; set; } = null!;
}
