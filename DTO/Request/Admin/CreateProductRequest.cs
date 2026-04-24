using System.ComponentModel.DataAnnotations;

namespace MyOwnLearning.DTO.Request.Admin
{
    public class CreateProductRequest
    {
        public string ProductName { get; set; } = null!;
        public int BrandId { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public string? MainImageUrl { get; set; }
        public decimal? DiscountPrice { get; set; }

        public List<CreateProductDetailRequest> ProductDetailRequests { get; set; } = new List<CreateProductDetailRequest>();
    }

    public class CreateProductDetailRequest
    {
        public string? WeightClass { get; set; }

        public string? GripSize { get; set; }

        public string? BalancePoint { get; set; }

        public string? Stiffness { get; set; }

        public int? MaxTension { get; set; }

        public decimal Price { get; set; }

        public int? StockQuantity { get; set; }


        [Required(ErrorMessage = "Mã Serial Number của phân loại không được để trống!")]
        public string SerialNumber { get; set; } = null!;
    }
}
