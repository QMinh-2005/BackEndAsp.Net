using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class ProductImage
{
    public int ImageId { get; set; }

    public int? ProductId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int? DisplayOrder { get; set; }

    public virtual Product? Product { get; set; }
}
