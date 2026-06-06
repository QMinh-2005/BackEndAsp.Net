namespace MyOwnLearning.DTO.Response.Admin
{
    public class LowStockVariantResponse
    {
        public int DetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public string VariantInfo { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int Threshold { get; set; }
    }
}
