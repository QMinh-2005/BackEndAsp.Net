using System.Security.Claims;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IProductDetailRepository _productDetailRepository;
        private readonly IVoucherService _voucherService;

        public OrderController(IOrderService orderService, IProductDetailRepository productDetailRepository, IVoucherService voucherService)
        {
            _orderService = orderService;
            _productDetailRepository = productDetailRepository;
            _voucherService = voucherService;
        }
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrder()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // SỬA: UserId trong model là int, nên ta dùng int.TryParse
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            var orders = await _orderService.GetMyOrdersAsync(userId);

            return Ok(orders);
        }
        [HttpGet("all-orders")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (orders, totalCount) = await _orderService.GetAllOrdersAsync(page, pageSize);
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            return Ok(new
            {
                Orders = orders,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        [HttpGet("admin/{orderId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrderDetailForAdmin(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderDetailForAdminAsync(orderId);
                return Ok(order);
            }
            catch (Exception ex)

            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPost("preview")]
        [Authorize]
        public async Task<IActionResult> PreviewOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Tạo list giả lập để truyền vào VoucherService
                var tempOrderItems = new List<OrderDetail>();
                decimal subTotal = 0;

                foreach (var od in request.OrderDetails)
                {
                    var productDetail = await _productDetailRepository.getProductDetailByIdAsync(od.DetailId);
                    if (productDetail == null) continue;

                    // Tính giá ưu tiên giống hệt trong OrderService
                    decimal currentPrice = productDetail.Price > 0
                        ? productDetail.Price
                        : (productDetail.Product?.DiscountPrice ?? productDetail.Product?.BasePrice ?? 0);

                    tempOrderItems.Add(new OrderDetail
                    {
                        DetailId = od.DetailId,
                        Quantity = od.Quantity,
                        UnitPrice = currentPrice
                    });

                    subTotal += currentPrice * od.Quantity;
                }

                // Gọi VoucherService để kiểm tra và tính toán giảm giá
                var voucherResult = await _voucherService.ValidateAndCalculateDiscountAsync(
                    userId,
                    request.VoucherIds ?? new List<int>(),
                    tempOrderItems,
                    request.PaymentMethod
                );

                // Trả về cho FE tổng quát số tiền
                return Ok(new
                {
                    SubTotal = subTotal,
                    TotalDiscount = voucherResult.TotalDiscount,
                    FinalAmount = subTotal - voucherResult.TotalDiscount,
                    IsValid = voucherResult.IsValid,
                    ErrorMessage = voucherResult.ErrorMessage,
                    AppliedVouchers = voucherResult.AppliedVoucherDetails
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
            try
            {
                var order = await _orderService.CreateOrderAsync(userId, request);
                return Ok(order);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPut("updateStatus/{orderId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var updatedOrder = await _orderService.UpdateOrderStatusAsync(orderId, request);
                return Ok(updatedOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("admin/{orderId:int}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelOrderByAdmin(int orderId, [FromBody] CancelOrderRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdClaim) || !int.TryParse(adminIdClaim, out var adminId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                var canceledOrder = await _orderService.CancelOrderByAdminAsync(orderId, adminId, request.Reason);
                return Ok(canceledOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("cancel-my-order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CancelMyOrder(int orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { Message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });

            try
            {
                var canceledOrder = await _orderService.CancelMyOrderAsync(orderId, userId);
                return Ok(canceledOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpGet("all-orders-by-status/{statusId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrdersByStatus(int statusId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var (orders, totalCount) = await _orderService.GetOrdersByStatusIdAsync(statusId, page, pageSize);
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                return Ok(new
                {
                    Orders = orders,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpGet("admin-search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchOrderAdmin([FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] DateTime? orderDate, [FromQuery] int? statusId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var (orders, totalCount) = await _orderService.SearchOrderAdminAsync(minPrice, maxPrice, orderDate, statusId, page, pageSize);
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                return Ok(new
                {
                    Orders = orders,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
