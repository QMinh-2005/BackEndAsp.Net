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

    public string? ImageUrl { get; set; }

    public virtual OrderDetail OrderDetail { get; set; } = null!;
}
