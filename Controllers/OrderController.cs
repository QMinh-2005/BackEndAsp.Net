using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request.Customer;
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
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
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
    }
}