using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class ProductDetailRepository : Repository<ProductDetail>, IProductDetailRepository
    {
        public ProductDetailRepository(WebBadmintonContext context) : base(context)
        {
        }
        public async Task<ProductDetail> getProductDetailByIdAsync(int detailId)
        {
            return await _dbset
                .Include(p => p.Product)
                .Include(s => s.ProductSerials)
                .FirstOrDefaultAsync(s => s.DetailId == detailId);
        }
        public async Task<ProductDetail> getProductDetailWithSerialNumberAsync(int detailId)
        {
            return await _dbset
                .Include(s => s.ProductSerials)
                .FirstOrDefaultAsync(s => s.DetailId == detailId);
        }
    }
}
