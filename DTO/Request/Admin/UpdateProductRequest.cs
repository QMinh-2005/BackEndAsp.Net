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

    }
}
