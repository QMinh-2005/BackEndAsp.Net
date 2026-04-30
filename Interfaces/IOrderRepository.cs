using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {

        Task<List<Order>> GetOrdersByUserIdAsync(int userId);
        Task<List<Order>> GetAllOrdersWithDetailsAsync();
        Task<Order> CreateOrderAsync(int userId, CreateOrderRequest request);
        Task<Order> UpdateStatusOrderAsync(int orderId, string newStatus);
    }
}
