namespace MyOwnLearning.DTO.Response.Customer
{
    public class ReviewResponse
    {
        public int ReviewId { get; set; }
        public int OrderDetailId { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? ReviewDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsVisible { get; set; }
        public List<string> Images { get; set; } = new List<string>();
    }
}
