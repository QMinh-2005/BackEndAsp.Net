using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int? OrderId { get; set; }

    public int? DetailId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public bool? IsStringingService { get; set; }

    public string? StringBrand { get; set; }

    public decimal? TensionKg { get; set; }

    public int? StringerId { get; set; }

    public virtual ProductDetail? Detail { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Review? Review { get; set; }

    public virtual User? Stringer { get; set; }
}
