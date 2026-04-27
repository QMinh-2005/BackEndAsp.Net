using MyOwnLearning.Interfaces;

namespace MyOwnLearning.Service
{
    public interface IOrderService
    {
    }
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }
    }
}
