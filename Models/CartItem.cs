using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int CartId { get; set; }

    public int DetailId { get; set; }

    public int Quantity { get; set; }

    public DateTime? AddedDate { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual ProductDetail Detail { get; set; } = null!;
}
