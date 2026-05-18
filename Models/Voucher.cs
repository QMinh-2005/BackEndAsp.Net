using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string VoucherCode { get; set; } = null!;

    public string? Description { get; set; }

    public decimal DiscountValue { get; set; }

    public bool? IsPercent { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public decimal? MinOrderValue { get; set; }

    public bool? IsGlobal { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public int MaxUsagePerUser { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();

    public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();

    public virtual ICollection<VoucherCondition> VoucherConditions { get; set; } = new List<VoucherCondition>();

    public virtual ICollection<VoucherPaymentMethod> VoucherPaymentMethods { get; set; } = new List<VoucherPaymentMethod>();
}
