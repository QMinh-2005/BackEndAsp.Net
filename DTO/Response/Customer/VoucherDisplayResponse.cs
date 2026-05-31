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
        public List<string> AllowedPaymentMethods { get; set; } = new List<string>();
        // ✅ BỔ SUNG 2 TRƯỜNG NÀY ĐỂ FRONTEND XỬ LÝ SÁNG / TỐI
        public bool IsEligible { get; set; } = true; // true = Sáng (Dùng được), false = Tối (Bị khóa)
        public string? DisabledReason { get; set; }   // Lý do vì sao mã này bị tối hóa
    }
}
