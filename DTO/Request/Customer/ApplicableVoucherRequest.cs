namespace MyOwnLearning.DTO.Request.Customer
{
    public class ApplicableVoucherRequest
    {
        public string? PaymentMethod { get; set; }
        public List<OrderItemRequest> OrderItems { get; set; } = new();
    }
    public class OrderItemRequest
    {
        public int DetailId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
