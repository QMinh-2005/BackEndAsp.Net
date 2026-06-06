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
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetReviewsByProductId(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var (reviews, averageRating, totalCount) = await _reviewService.GetReviewsByProductIdAsync(productId, page, pageSize);
                return Ok(new
                {
                    Message = "Thành công",
                    Items = reviews,
                    AverageRating = averageRating,
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

        [HttpGet("my-reviews")]
        [Authorize]
        public async Task<IActionResult> GetMyReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                var (reviews, totalCount) = await _reviewService.GetMyReviewsAsync(userId, page, pageSize);
                return Ok(new
                {
                    Message = "Thành công",
                    Items = reviews,
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

        [HttpGet("my-reviewable-items")]
        [Authorize]
        public async Task<IActionResult> GetMyReviewableItems([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                var (items, totalCount) = await _reviewService.GetMyReviewableOrderDetailsAsync(userId, page, pageSize);
                return Ok(new
                {
                    Message = "Thành công",
                    Items = items,
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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var review = await _reviewService.CreateReviewAsync(userId, request);
                return Ok(new { Message = "Đánh giá sản phẩm thành công.", Data = review });
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

        [HttpPut("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> UpdateMyReview(int reviewId, [FromBody] UpdateReviewRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var review = await _reviewService.UpdateMyReviewAsync(userId, reviewId, request);
                return Ok(new { Message = "Cập nhật đánh giá thành công.", Data = review });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
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

        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> DeleteMyReview(int reviewId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                await _reviewService.DeleteMyReviewAsync(userId, reviewId);
                return Ok(new { Message = "Xóa đánh giá thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
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

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReviewsForAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isVisible = null,
            [FromQuery] int? rating = null,
            [FromQuery] int? productId = null,
            [FromQuery] int? userId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var (reviews, totalCount) = await _reviewService.GetReviewsForAdminAsync(
                    page,
                    pageSize,
                    isVisible,
                    rating,
                    productId,
                    userId,
                    fromDate,
                    toDate);
                return Ok(new
                {
                    Message = "Thành công",
                    Items = reviews,
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

        [HttpPut("admin/{reviewId}/visibility")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetReviewVisibility(int reviewId, [FromBody] UpdateReviewVisibilityRequest request)
        {
            try
            {
                var review = await _reviewService.SetReviewVisibilityAsync(reviewId, request.IsVisible);
                return Ok(new { Message = "Cập nhật trạng thái hiển thị đánh giá thành công.", Data = review });
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
