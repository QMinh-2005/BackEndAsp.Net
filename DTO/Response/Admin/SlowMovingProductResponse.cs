namespace MyOwnLearning.DTO.Response.Admin
{
    public class SlowMovingProductResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? MainImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public int TotalStock { get; set; }
        public int TotalSold { get; set; }
        public DateTime? LastSoldDate { get; set; }
        public int DaysWithoutSale { get; set; }
    }
}
