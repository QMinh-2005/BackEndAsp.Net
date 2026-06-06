using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class OrderDeliveryProof
{
    public int ProofId { get; set; }

    public int OrderId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
