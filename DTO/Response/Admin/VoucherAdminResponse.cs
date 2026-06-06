namespace MyOwnLearning.DTO.Response.Admin
{
    public class VoucherAdminResponse
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DiscountValue { get; set; }
        public bool? IsPercent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsGlobal { get; set; }
        public bool IsActive { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public int MaxUsagePerUser { get; set; }
        public int SavedCount { get; set; }
        public int ConditionCount { get; set; }
        public List<string> AllowedPaymentMethods { get; set; } = new();
        public List<VoucherConditionAdminResponse> Conditions { get; set; } = new();
    }

    public class VoucherConditionAdminResponse
    {
        public int ConditionId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? BrandId { get; set; }
        public string? BrandName { get; set; }
    }
}
