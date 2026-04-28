using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(WebBadmintonContext context) : base(context)
        {
        }
        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _dbset.Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Detail)
                        .ThenInclude(d => d.Product)
                .ToListAsync();
        }

    }
}
