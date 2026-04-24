namespace MyOwnLearning.DTO.Response.Customer
{
    public class ProductHomeResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string MainImageUrl { get; set; } = null!;
        public string? CategoryName { get; set; }

        // Xử lý Giá
        public decimal BasePrice { get; set; } // Giá gốc (Bị gạch ngang)
        public decimal SellingPrice { get; set; }  // Giá bán thực tế (Màu cam)

        // Phần trăm giảm giá (Hiển thị nhãn -13%)
        public int DiscountPercent { get; set; }

        // Nhãn "Bán chạy"
        public bool IsBestSeller { get; set; }
    }
}
