namespace MyOwnLearning.DTO.Response.Customer
{
    public class VoucherResponse
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

        public int MaxUsagePerUser { get; set; }
        public List<string> AllowedPaymentMethods { get; set; } = new List<string>();
    }
}
