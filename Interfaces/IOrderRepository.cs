using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {

        Task<List<Order>> GetOrdersByUserIdAsync(int userId);
    }
}
