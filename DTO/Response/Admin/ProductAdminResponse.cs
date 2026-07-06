namespace MyOwnLearning.DTO.Response.Admin
{
    public class ProductAdminResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? MainImageUrl { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountPrice { get; set; }

        public decimal? delta {get; set;}
        public int DiscountPercent { get; set; }

        public string BrandName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;

        public int VariantsCount { get; set; }
        public int TotalStock { get; set; }
        public int SoldQuantity { get; set; }
    }
}
