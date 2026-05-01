using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class ProductSpecification
{
    public int SpecId { get; set; }

    public int? ProductId { get; set; }

    public string SpecName { get; set; } = null!;

    public string SpecValue { get; set; } = null!;

    public int? DisplayOrder { get; set; }

    public virtual Product? Product { get; set; }
}
