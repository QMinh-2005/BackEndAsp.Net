using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int? BrandId { get; set; }

    public int? CategoryId { get; set; }

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();

    public virtual ICollection<VoucherCondition> VoucherConditions { get; set; } = new List<VoucherCondition>();
}
