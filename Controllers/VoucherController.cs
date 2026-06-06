using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Request.Customer;
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
        [HttpPost("my-voucher")]
        [Authorize]
        public async Task<IActionResult> GetAvailableVouchers([FromBody] ApplicableVoucherRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { Message = "Vui lòng đăng nhập." });

                var result = await _voucherService.GetAvailableVouchersForUserAsync(userId, request);
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

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetVouchersForAdmin(
            string? keyword,
            bool? isActive,
            bool? isGlobal,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var (vouchers, totalCount) = await _voucherService.GetVouchersForAdminAsync(
                    keyword,
                    isActive,
                    isGlobal,
                    fromDate,
                    toDate,
                    page,
                    pageSize);

                return Ok(new
                {
                    Total = totalCount,
                    Data = vouchers,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("admin/{voucherId:int}/active")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetVoucherActive(int voucherId, [FromBody] SetActiveRequest request)
        {
            var updated = await _voucherService.SetVoucherActiveAsync(voucherId, request.IsActive);
            if (!updated)
            {
                return NotFound(new { message = "Không tìm thấy voucher." });
            }

            return Ok(new
            {
                message = request.IsActive ? "Đã kích hoạt voucher." : "Đã tắt voucher.",
                VoucherId = voucherId,
                request.IsActive
            });
        }

    }
}
