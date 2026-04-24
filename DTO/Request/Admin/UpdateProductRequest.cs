using System.ComponentModel.DataAnnotations;

namespace MyOwnLearning.DTO.Request.Admin
{
    public class UpdateProductRequest
    {
        public string? ProductName { get; set; } = null!;
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public string? Description { get; set; }
        public decimal? BasePrice { get; set; }
        public string? MainImageUrl { get; set; }
        public decimal? DiscountPrice { get; set; }

        public List<UpdateProductDetailRequest> ProductDetailRequests { get; set; } = new List<UpdateProductDetailRequest>();
    }
    public class UpdateProductDetailRequest
    {
        public string? WeightClass { get; set; }

        public string? GripSize { get; set; }

        public string? BalancePoint { get; set; }

        public string? Stiffness { get; set; }

        public int? MaxTension { get; set; }

        public decimal? Price { get; set; }

        public int? StockQuantity { get; set; }


        public string? SerialNumber { get; set; }
    }
}
