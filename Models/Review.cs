using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int OrderDetailId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? ReviewDate { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsVisible { get; set; }

    public virtual OrderDetail OrderDetail { get; set; } = null!;

    public virtual ICollection<ReviewImage> ReviewImages { get; set; } = new List<ReviewImage>();
}
