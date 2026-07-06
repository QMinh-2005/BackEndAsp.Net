namespace MyOwnLearning.DTO.Response.Admin
{
    public class CategoryResponse
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }

    public class CategoryProductCountResponse
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public int ProductCount { get; set; }
    }
}
