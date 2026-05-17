namespace MyOwnLearning.DTO.Request.Admin
{
    public class CreateProductImageRequest
    {
        public string ImageUrl { get; set; } = null!;
        public bool IsMain { get; set; }
    }
}
