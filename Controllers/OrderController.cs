using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        [HttpGet]
        [Permission("ORDER", "VIEW")]
        public async Task<IActionResult> GetAllOrders()
        {
            //var orders = await _orderService.GetAllOrdersAsync();
            //return Ok(orders);
        }
        [HttpGet("{orderId}")]
        [Permission("ORDER", "VIEW")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            //var order = await _orderService.GetOrderByIdAsync(orderId);
            //if (order == null) { return NotFound(new { Message = "Không tìm thấy đơn hàng." }); }
            //return Ok(order);
        }
        [HttpPost]
        [Permission("ORDER", "CREATE")]
        public async Task<IActionResult> CreateOrder()
        {
            //var newOrder = await _orderService.CreateOrderAsync();
            //return Ok(newOrder);
        }
    }
}
