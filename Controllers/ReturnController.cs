using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnController : ControllerBase
    {
        private readonly IReturnRequestService _returnRequestService;

        public ReturnController(IReturnRequestService returnRequestService)
        {
            _returnRequestService = returnRequestService;
        }

        [HttpGet("reasons")]
        public async Task<IActionResult> GetReturnReasons()
        {
            var reasons = await _returnRequestService.GetReturnReasonsAsync();
            return Ok(reasons);
        }

        [HttpGet("orders/{orderId:int}/delivery-proofs")]
        [Authorize]
        public async Task<IActionResult> GetDeliveryProofs(int orderId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                var proofs = await _returnRequestService.GetDeliveryProofsAsync(orderId, userId, User.IsInRole("Admin"));
                return Ok(proofs);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("request")]
        [Authorize]
        public async Task<IActionResult> CreateReturnRequest([FromBody] CreateReturnRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                var result = await _returnRequestService.CreateReturnRequestAsync(userId, request);
                return Ok(new { Message = "Gửi yêu cầu trả hàng/hoàn tiền thành công.", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-requests")]
        [Authorize]
        public async Task<IActionResult> GetMyReturnRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                var (requests, totalCount) = await _returnRequestService.GetMyReturnRequestsAsync(userId, page, pageSize);
                return Ok(new
                {
                    Items = requests,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-requests/{returnRequestId:int}")]
        [Authorize]
        public async Task<IActionResult> GetMyReturnRequestDetail(int returnRequestId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                var request = await _returnRequestService.GetMyReturnRequestDetailAsync(userId, returnRequestId);
                return Ok(request);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("admin/orders/{orderId:int}/delivery-proofs")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddDeliveryProof(int orderId, IFormFile file, [FromForm] string? note = null)
        {
            try
            {
                var proof = await _returnRequestService.AddDeliveryProofAsync(orderId, file, note);
                return Ok(new { Message = "Thêm ảnh minh chứng giao hàng thành công.", Data = proof });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("admin/requests")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReturnRequestsForAdmin(
            [FromQuery] string? status = null,
            [FromQuery] int? orderId = null,
            [FromQuery] int? userId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var (requests, totalCount) = await _returnRequestService.GetReturnRequestsForAdminAsync(
                    status,
                    orderId,
                    userId,
                    fromDate,
                    toDate,
                    page,
                    pageSize);

                return Ok(new
                {
                    Items = requests,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("admin/requests/{returnRequestId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReturnRequestDetailForAdmin(int returnRequestId)
        {
            try
            {
                var request = await _returnRequestService.GetReturnRequestDetailForAdminAsync(returnRequestId);
                return Ok(request);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("admin/requests/{returnRequestId:int}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveReturnRequest(int returnRequestId, [FromBody] ReviewReturnRequestRequest request)
        {
            try
            {
                var result = await _returnRequestService.ApproveReturnRequestAsync(returnRequestId, request);
                return Ok(new { Message = "Đã chấp thuận yêu cầu trả hàng/hoàn tiền.", Data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("admin/requests/{returnRequestId:int}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectReturnRequest(int returnRequestId, [FromBody] ReviewReturnRequestRequest request)
        {
            try
            {
                var result = await _returnRequestService.RejectReturnRequestAsync(returnRequestId, request);
                return Ok(new { Message = "Đã từ chối yêu cầu trả hàng/hoàn tiền.", Data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("admin/requests/{returnRequestId:int}/mark-refunded")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkRefunded(int returnRequestId, [FromBody] MarkReturnRefundedRequest request)
        {
            try
            {
                var result = await _returnRequestService.MarkRefundedAsync(returnRequestId, request);
                return Ok(new { Message = "Đã đánh dấu hoàn tiền cho yêu cầu.", Data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        private bool TryGetUserId(out int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out userId);
        }
    }
}
