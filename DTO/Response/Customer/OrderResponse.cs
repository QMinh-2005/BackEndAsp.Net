namespace MyOwnLearning.DTO.Response
{
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? TotalDiscount { get; set; } // THÊM MỚI
        public decimal? FinalAmount { get; set; }
        public int? StatusId { get; set; }
        public string? Status { get; set; }
        public decimal? ShippingFee { get; set; }
        public string ReceiverName { get; set; }
        public string PhoneNumber { get; set; }
        public string ShippingAddress { get; set; }
        public string? Note { get; set; }
        public string? CancelReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public int? CancelledByUserId { get; set; }

        public string PaymentMethod { get; set; }
        public List<OrderDetailResponse> OrderDetails
        { get; set; } = new List<OrderDetailResponse>();
        public List<AppliedVoucherResponse> AppliedVouchers { get; set; } = new List<AppliedVoucherResponse>();
    }
    public class OrderDetailResponse
    {
        public int OrderDetailId { get; set; }
        public int? DetailId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public bool? IsStringingService { get; set; }
        public string? StringBrand { get; set; }
        public decimal? TensionKg { get; set; }
        public string? ProductName { get; set; }
        public List<string> SerialNumbers { get; set; } = new List<string>();
    }
    public class AppliedVoucherResponse
    {
        public string VoucherCode { get; set; } = null!;
        public decimal AppliedDiscount { get; set; }
    }
}
