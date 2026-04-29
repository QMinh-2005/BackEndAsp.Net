using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface ICartRepository : IRepository<Cart>
    {
        Task<Cart?> GetCartByUserIdAsync(int userId);
    }
}
