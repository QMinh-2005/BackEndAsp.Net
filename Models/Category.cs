using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Slug { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<VoucherCondition> VoucherConditions { get; set; } = new List<VoucherCondition>();
}
