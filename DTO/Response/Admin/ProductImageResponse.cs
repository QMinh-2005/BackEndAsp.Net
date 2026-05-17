namespace MyOwnLearning.DTO.Response.Admin
{
    public class ProductImageResponse
    {
        public int ImageId { get; set; }

        public string ImageUrl { get; set; } = null!;

        public int? DisplayOrder { get; set; }
        public bool? IsMain { get; set; }
    }
}
