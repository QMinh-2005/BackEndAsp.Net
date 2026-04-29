using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class CartRepository : Repository<Cart>, ICartRepository
    {
        public CartRepository(WebBadmintonContext context) : base(context)
        {
        }
        public async Task<Cart?> GetCartByUserIdAsync(int userId)
        {
            return await _dbset
                .Include(c => c.CartItems)
                    .ThenInclude(cd => cd.Detail)
                        .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}