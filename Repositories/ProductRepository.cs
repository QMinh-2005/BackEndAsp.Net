using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Admin;
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
        public virtual async Task<(List<Product> products, int TotalCount)> SearchAsync(string? keyword, string? categorySlug, string? brandSlug, decimal? minPrice, decimal? maxPrice, bool? Voucher, int page, int pageSize)
        {
            var query = _dbset
        .Include(p => p.Category)
        .Include(p => p.Brand)
        .AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(p => p.ProductName.Contains(keyword));
            }
            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                query = query.Where(p => p.Category != null && p.Category.Slug == categorySlug);
            }

            // 3. Lọc theo Thương hiệu (Ví dụ: adidas)
            if (!string.IsNullOrWhiteSpace(brandSlug))
            {
                query = query.Where(p => p.Brand != null && p.Brand.Slug == brandSlug);
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

            var products = await query
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (products, TotalCount);
        }

        public async Task<List<Product>> GetProductsForHomePageAsync(List<int> categoryIds)
        {
            // Lấy ra danh sách sản phẩm thuộc các Category truyền vào
            // Chúng ta sử dụng Include(p => p.Category) để có tên danh mục phục vụ mapping ở Service
            var query = _dbset.Include(p => p.Category)
                              .Where(p => categoryIds.Contains(p.CategoryId ?? 0))
                              .AsQueryable();

            // Để lấy "N sản phẩm cho mỗi danh mục" trong 1 câu query duy nhất của EF Core 
            // thường khá phức tạp. Cách đơn giản và hiệu quả nhất cho trang chủ là:
            var result = await query
                .OrderByDescending(p => p.SoldQuantity)
                .ToListAsync();

            // Sau đó phân nhóm và lấy top N tại đây (hoặc để Service xử lý tùy bạn)
            return result.GroupBy(p => p.CategoryId)
                         .SelectMany(g => g.Take(6))
                         .ToList();
        }
        public async Task<(List<Product> products, int TotalCount)> GetProductsByCategorySlugAsync(string categorySlug, int page, int pageSize)
        {
            var query = _dbset.Include(p => p.Category)
                              .Where(p => p.Category != null && p.Category.Slug == categorySlug)
                              .AsQueryable();
            var products = await query
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var totalCount = await query.CountAsync();
            return (products, totalCount);
        }
        public async Task<Product?> GetProductDetailBySlugAsync(string slug)
        {
            return await _dbset
                .Include(p => p.ProductImages)
                .Include(p => p.ProductDetails)
                .FirstOrDefaultAsync(p => p.Slug == slug);
        }
    }
}
