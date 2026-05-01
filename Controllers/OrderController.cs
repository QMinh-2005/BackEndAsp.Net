using System.Security.Claims;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
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
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] int newOrderStatusId)
        {
            try
            {
                var updatedOrder = await _orderService.UpdateOrderStatusAsync(orderId, newOrderStatusId);
                return Ok(updatedOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPut("cancel-my-order/{orderId}")]
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
    }
}