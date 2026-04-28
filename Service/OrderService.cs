using MyOwnLearning.DTO.Response;
using MyOwnLearning.Interfaces;

namespace MyOwnLearning.Service
{
    public interface IOrderService
    {
        Task<List<OrderResponse>> GetMyOrdersAsync(int userId);
    }
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }
        public async Task<List<OrderResponse>> GetMyOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);

            // Chuyển đổi dữ liệu từ Entity sang DTO
            var orderResponses = orders.Select(o => new OrderResponse
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailResponse
                {
                    OrderDetailId = od.OrderDetailId,
                    DetailId = od.DetailId,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    IsStringingService = od.IsStringingService,
                    StringBrand = od.StringBrand,
                    TensionKg = od.TensionKg,
                    ProductName = od.Detail?.Product?.ProductName
                }).ToList()
            }).ToList();

            return orderResponses;
        }
    }
}
