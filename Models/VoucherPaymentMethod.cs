using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class VoucherPaymentMethod
{
    public int Id { get; set; }

    public int VoucherId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
