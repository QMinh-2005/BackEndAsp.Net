using System.ComponentModel.DataAnnotations;

namespace MyOwnLearning.DTO.Request.Customer
{
    public class CreateReviewRequest
    {
        [Required]
        public int OrderDetailId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao.")]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        // Danh sách URL ảnh (sau khi Frontend đã upload lên Cloudinary và lấy link về)
        public List<string>? ImageUrls { get; set; }
    }
}
