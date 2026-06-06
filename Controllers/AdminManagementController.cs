using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [ApiController]
    [Route("api/admin/management")]
    [Authorize(Roles = "Admin")]
    public class AdminManagementController : ControllerBase
    {
        private readonly IAdminManagementService _adminManagementService;

        public AdminManagementController(IAdminManagementService adminManagementService)
        {
            _adminManagementService = adminManagementService;
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAdminAuditLogs(
            int? adminId,
            string? module,
            string? action,
            string? targetType,
            int? targetId,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var (logs, totalCount) = await _adminManagementService.GetAdminAuditLogsAsync(
                    adminId,
                    module,
                    action,
                    targetType,
                    targetId,
                    fromDate,
                    toDate,
                    page,
                    pageSize);

                return Ok(new
                {
                    Total = totalCount,
                    Data = logs,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("audit-logs")]
        public async Task<IActionResult> CreateAdminAuditLog([FromBody] CreateAdminAuditLogRequest request)
        {
            try
            {
                FillCurrentAdminInfo(request);
                var log = await _adminManagementService.LogAdminActionAsync(request);
                return Ok(new { message = "Ghi lịch sử thao tác thành công.", data = log });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("alerts/summary")]
        public async Task<IActionResult> GetAdminAlertSummary(
            int lowStockThreshold = 5,
            int voucherExpiringDays = 7,
            int lowRatingReviewDays = 7)
        {
            var result = await _adminManagementService.GetAdminAlertSummaryAsync(
                lowStockThreshold,
                voucherExpiringDays,
                lowRatingReviewDays);

            return Ok(result);
        }

        [HttpGet("products/slow-moving")]
        public async Task<IActionResult> GetSlowMovingProducts(
            int daysWithoutSale = 30,
            int page = 1,
            int pageSize = 10)
        {
            var (products, totalCount) = await _adminManagementService.GetSlowMovingProductsAsync(
                daysWithoutSale,
                page,
                pageSize);

            return Ok(new
            {
                Total = totalCount,
                Data = products,
                Page = page,
                PageSize = pageSize
            });
        }

        private void FillCurrentAdminInfo(CreateAdminAuditLogRequest request)
        {
            if (!request.AdminId.HasValue)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    request.AdminId = userId;
                }
            }

            if (string.IsNullOrWhiteSpace(request.AdminEmail))
            {
                request.AdminEmail = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst(ClaimTypes.Name)?.Value;
            }
        }
    }
}
