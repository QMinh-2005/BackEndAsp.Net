namespace MyOwnLearning.DTO.Response.Admin
{
    public class OrderSummaryResponse
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? ReceiverName { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal? FinalAmount { get; set; }
        public int? StatusId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string FirstProductName { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public string? CancelReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public int? CancelledByUserId { get; set; }
    }
}
