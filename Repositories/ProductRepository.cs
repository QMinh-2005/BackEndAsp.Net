using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(WebBadmintonContext context) : base(context)
        {
        }
        public virtual async Task<(List<Product> products, int TotalCount)> GetAll()
        {
            var query = _dbset.AsQueryable();
            var totalCount = await query.CountAsync();
            var products = await query.OrderByDescending(x => x.ProductId).ToListAsync();
            return (products, totalCount);
        }
        public virtual async Task<(List<Product> products, int TotalCount)> SeacrhAsync(string? keyword, decimal? minPrice, decimal? maxPrice, bool? Voucher)
        {
            var query = _dbset.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(p => p.ProductName.Contains(keyword));
            }
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.BasePrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.BasePrice <= maxPrice.Value);
            }
            if (Voucher.HasValue && Voucher.Value == true)
            {
                query = query.Where(p => p.VoucherConditions.Any());
            }
            int TotalCount = await query.CountAsync();
            var products = await query.ToListAsync();
            return (products, TotalCount);
        }
    }
}
