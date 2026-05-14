using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class OrderVoucher
{
    public int OrderVoucherId { get; set; }

    public int OrderId { get; set; }

    public int VoucherId { get; set; }

    public decimal AppliedDiscount { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
