using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class UserVoucher
{
    public int UserVoucherId { get; set; }

    public int UserId { get; set; }

    public int VoucherId { get; set; }

    public DateTime? SavedDate { get; set; }

    public DateTime? UsedDate { get; set; }

    public int CurrentUsageCount { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
