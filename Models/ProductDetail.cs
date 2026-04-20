using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class ProductDetail
{
    public int DetailId { get; set; }

    public int ProductId { get; set; }

    public string? WeightClass { get; set; }

    public string? GripSize { get; set; }

    public string? BalancePoint { get; set; }

    public string? Stiffness { get; set; }

    public int? MaxTension { get; set; }

    public decimal Price { get; set; }

    public int? StockQuantity { get; set; }

    public string SerialNumber { get; set; } = null!;

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Product Product { get; set; } = null!;
}
