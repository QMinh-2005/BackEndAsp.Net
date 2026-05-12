using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class ProductSerial
{
    public int SerialId { get; set; }

    public int DetailId { get; set; }

    public string SerialNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? ImportDate { get; set; }

    public int? OrderDetailId { get; set; }

    public virtual ProductDetail Detail { get; set; } = null!;

    public virtual OrderDetail? OrderDetail { get; set; }
}
