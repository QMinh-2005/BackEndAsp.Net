using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        //[HttpGet("{orderId}")]
        //[Permission("ORDER", "VIEW")]
        //public async Task<IActionResult> GetOrderById(int orderId)
        //{
        //    //var order = await _orderService.GetOrderByIdAsync(orderId);
        //    //if (order == null) { return NotFound(new { Message = "Không tìm thấy đơn hàng." }); }
        //    //return Ok(order);
        //}
        //[HttpPost]
        //[Permission("ORDER", "CREATE")]
        //public async Task<IActionResult> CreateOrder()
        //{
        //    //var newOrder = await _orderService.CreateOrderAsync();
        //    //return Ok(newOrder);
        //}
    }
}
