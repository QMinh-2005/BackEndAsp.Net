using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetAvailableVouchers()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { Message = "Vui lòng đăng nhập." });

            var result = await _voucherService.GetAvailableVouchersForUserAsync(userId);
            return Ok(result);
        }
    }
}
