using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        // Gọi API này khi người dùng click vào Dropdown chọn Voucher ở trang Thanh toán
        [HttpGet("my-voucher")]
        public async Task<IActionResult> GetAvailableVouchers([FromQuery] string? paymentMethod)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { Message = "Vui lòng đăng nhập." });

                var result = await _voucherService.GetAvailableVouchersForUserAsync(userId, paymentMethod);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // 1. Dành cho người dùng: Xem tất cả voucher đang khả dụng để lưu
        [HttpGet("all-available")]
        public async Task<IActionResult> GetAllAvailable()
        {
            try
            {
                var vouchers = await _voucherService.GetAllVouchersForUserAsync();
                return Ok(new { message = "Lấy thành công tât cả các Voucher", data = vouchers });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("save/{voucherId}")]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> SaveVoucher(int voucherId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { Message = "Vui lòng đăng nhập." });
                await _voucherService.SaveVoucherAsync(userId, voucherId);
                return Ok(new { message = "Lưu voucher thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // 3. Dành cho Admin: Thêm Voucher mới
        [HttpPost("admin/add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateVoucher([FromBody] VoucherCreateRequest request)
        {
            var result = await _voucherService.CreateVoucherAsync(request);
            return Ok(new { message = "Tạo thành công Voucher", VoucherId = result.VoucherId, VoucherCode = result.VoucherCode });
        }

    }
}
