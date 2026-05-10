using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.Enums;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetaDataController : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetMetaData()
        {
            try
            {
                var responseData = new
                {
                    WeightClassess = VariantAttributes.WeightClasses.Select(item => new { Label = item, Value = item }),
                    GripSizes = VariantAttributes.GripSizes.Select(item => new { Label = item, Value = item }),
                    BalancePoints = VariantAttributes.BalancePoints.Select(item => new { Label = item, Value = item }),
                    Stiffness = VariantAttributes.Stiffness.Select(item => new { Label = item, Value = item }),
                    MaxTensions = new[] { new { Label = "N/A (Không áp dụng)", Value = (int?)null } }
                // Sau đó nối thêm mảng số từ VariantAttributes, tự động gắn thêm chữ " lbs" vào Label
                .Concat(VariantAttributes.MaxTension.Select(t => new
                {
                    Label = $"{t} lbs",
                    Value = (int?)t
                })),

                    SerialStatus = new[]
                {
                    new { Label = "Còn hàng", Value = ProductSerialStatus.InStock },
                    new { Label = "Đã bán", Value = ProductSerialStatus.Sold },
                    new { Label = "Lỗi/ Hỏng", Value = ProductSerialStatus.Defective },
                    new { Label = "Đã đặt", Value = ProductSerialStatus.Reserved }
                }
                };
                return Ok(new
                {
                    Message = "Lấy dữ liệu meta thành công.",
                    Data = responseData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "Đã xảy ra lỗi khi lấy dữ liệu meta.",
                    Error = ex.Message
                });
            }
        }
    }
}
