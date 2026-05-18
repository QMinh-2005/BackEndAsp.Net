namespace MyOwnLearning.DTO.Request.Admin
{
    public class VoucherCreateRequest
    {
        public string VoucherCode { get; set; }
        public string Description { get; set; }
        public decimal DiscountValue { get; set; }
        public bool IsPercent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MinOrderValue { get; set; }
        public bool IsGlobal { get; set; }
        public int MaxUsagePerUser { get; set; }
        public int? UsageLimit { get; set; } // Tổng lượt dùng toàn hệ thống

        // Danh sách điều kiện nếu có (Sản phẩm hoặc Danh mục cụ thể)
        public List<VoucherConditionRequest>? Conditions { get; set; }
        public List<string>? AllowedPaymentMethods { get; set; }
    }
    public class VoucherConditionRequest
    {
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
    }
}
