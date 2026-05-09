using System.ComponentModel.DataAnnotations;

namespace MyOwnLearning.DTO.Request.Admin
{
    public class CreateProductDetailRequest
    {

        public string? WeightClass { get; set; }

        public string? GripSize { get; set; }

        public string? BalancePoint { get; set; }

        public string? Stiffness { get; set; }

        public int? MaxTension { get; set; }
        [Required(ErrorMessage = "Phải nhập giá sản phẩm.")]

        public decimal Price { get; set; }

        [Required(ErrorMessage = "Phải nhập số lượng tồn kho.")]
        public int StockQuantity { get; set; }
    }
}
