using System.ComponentModel.DataAnnotations;

namespace MyOwnLearning.DTO.Request.Admin
{
    public class CreateProductSerialRequest
    {
        [Required(ErrorMessage = "Mã Serial Number không được để trống!")]
        public string SerialNumber { get; set; } = null!;

        [Required(ErrorMessage = "Trạng thái không được để trống!")]
        public string Status { get; set; } = "InStock";

        // Cho phép null, nếu FE không gửi thì BE tự lấy ngày giờ hiện tại
        public DateTime? ImportDate { get; set; }
    }
}
