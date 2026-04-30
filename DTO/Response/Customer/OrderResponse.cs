namespace MyOwnLearning.DTO.Response
{
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Status { get; set; }
        public decimal? ShippingFee { get; set; }
        public string ReceiverName { get; set; }
        public string PhoneNumber { get; set; }
        public string ShippingAddress { get; set; }
        public string? Note { get; set; }

        public string PaymentMethod { get; set; }
        public List<OrderDetailResponse> OrderDetails
        { get; set; } = new List<OrderDetailResponse>();
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
    }
}
