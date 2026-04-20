using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class VoucherCondition
{
    public int ConditionId { get; set; }

    public int? VoucherId { get; set; }

    public int? ProductId { get; set; }

    public int? CategoryId { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
