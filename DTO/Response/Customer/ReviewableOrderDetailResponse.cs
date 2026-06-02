namespace MyOwnLearning.DTO.Response.Customer
{
    public class ReviewableOrderDetailResponse
    {
        public int OrderDetailId { get; set; }
        public int? OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public string VariantInfo { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
