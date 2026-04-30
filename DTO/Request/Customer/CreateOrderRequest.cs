namespace MyOwnLearning.DTO.Request.Customer
{
    public class CreateOrderRequest
    {
        public string ShippingAddress { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string ReceiverName { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public List<CreateOrderDetailRequest> OrderDetails { get; set; } = new List<CreateOrderDetailRequest>();
    }
    public class CreateOrderDetailRequest
    {
        public int? DetailId { get; set; }
        public int Quantity { get; set; }
        public bool? IsStringingService { get; set; }
        public string? StringBrand { get; set; }
        public decimal? TensionKg { get; set; }
    }
}
