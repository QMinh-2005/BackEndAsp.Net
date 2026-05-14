namespace MyOwnLearning.DTO.Response.Customer
{
    public class VoucherDisplayResponse
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public string? Description { get; set; }
        public decimal DiscountValue { get; set; }
        public bool? IsPercent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal MinOrderValue { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsGlobal { get; set; }
    }
}
