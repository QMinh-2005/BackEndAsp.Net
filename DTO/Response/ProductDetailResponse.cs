using MyOwnLearning.Models;

namespace MyOwnLearning.DTO.Response
{
    public class ProductDetailResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int DiscountPercent { get; set; }
        public string? MainImageUrl { get; set; }
        public string? Description { get; set; }
        public bool IsAvailable { get; set; }
        public List<ProductImage> Imgaes { get; set; } = new List<ProductImage>();
        public List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }

    public class ProductVariant
    {
        public int DetailId { get; set; }

        public string? WeightClass { get; set; }
        public string? GripSize { get; set; }
        public string? BalancePoint { get; set; }
        public string? Stiffness { get; set; }
        public int? MaxTension { get; set; }

        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        public bool InStock { get; set; }
    }
}
