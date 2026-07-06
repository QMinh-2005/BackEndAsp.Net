using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response.Admin;
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
            var query = _dbset.Include(c => c.Category).Include(b => b.Brand).AsQueryable();
            var totalCount = await query.CountAsync();
            var products = await query.OrderByDescending(x => x.ProductId).ToListAsync();
            return (products, totalCount);
        }
        public async Task<bool> IsExistProduct(string productName)
        {
            var pro = await _dbset.FirstOrDefaultAsync(p => p.ProductName == productName);
            if (pro != null) { return true; }
            return false;
        }
        public virtual async Task<(List<Product> products, int TotalCount)> SearchAsync(string? categorySlug, string? brandSlug, string? keyword, decimal? minPrice, decimal? maxPrice, bool? Voucher, bool? isBestSeller, string? sortBy, int page, int pageSize)
        {
            var query = _dbset.AsQueryable();
            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                query = query.Where(p => p.Category != null && p.Category.Slug == categorySlug);
            }
            if (!string.IsNullOrWhiteSpace(brandSlug))
            {
                query = query.Where(p => p.Brand != null && p.Brand.Slug == brandSlug);
            }
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
            if (isBestSeller.HasValue && isBestSeller.Value == true)
            {
                query = query.Where(p => p.SoldQuantity >= 10);
            }

            int TotalCount = await query.CountAsync();
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.DiscountPrice != null ? p.DiscountPrice : p.BasePrice),
                "price_desc" => query.OrderByDescending(p => p.DiscountPrice != null ? p.DiscountPrice : p.BasePrice),
                "name_asc" => query.OrderBy(p => p.ProductName),
                "name_desc" => query.OrderByDescending(p => p.ProductName),
                "oldest" => query.OrderBy(p => p.ProductId),
                _ => query.OrderByDescending(p => p.ProductId)
            };
            var products = await query
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

        public async Task<(List<ProductDetail> productDetails, int TotalCount)> GetProductDetailsByIdAsync(int productId, int page, int pageSize)
        {
            var query = _context.Set<ProductDetail>()
                .Include(ps => ps.ProductSerials)
                .Where(p => p.ProductId == productId).AsQueryable();
            var productDetails = await query
                .OrderByDescending(pd => pd.DetailId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var totalCount = await query.CountAsync();
            return (productDetails, totalCount);
        }
        // Thêm vào ProductRepository.cs
        public async Task<Product?> GetProductForDeletionAsync(int productId)
        {
            return await _dbset
                .Include(p => p.ProductDetails)
                    .ThenInclude(d => d.ProductSerials)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }
        // ── Helpers dùng nội bộ ──────────────────────────────────────────────────
        private IQueryable<Product> AdminBaseQuery() =>
            _dbset.Include(p => p.Brand).Include(p => p.Category).Include(p => p.ProductDetails).AsQueryable();

        private async Task<(List<Product>, int)> PageAsync(IQueryable<Product> query, int page, int pageSize)
        {
            var count = await query.CountAsync();
            var items = await query.OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, count);
        }

        // ── Base: keyword + single category/brand ────────────────────────────────
        public async Task<(List<Product> products, int TotalCount)> GetProductsForAdminAsync(
            string? keyword, int? categoryId, int? brandId, int page, int pageSize)
        {
            var q = AdminBaseQuery();
            if (!string.IsNullOrWhiteSpace(keyword)) q = q.Where(p => p.ProductName.Contains(keyword));
            if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
            if (brandId.HasValue) q = q.Where(p => p.BrandId == brandId.Value);
            return await PageAsync(q, page, pageSize);
        }

        // ── Filter: chỉ lọc theo khoảng giá ─────────────────────────────────────
        public async Task<(List<Product> products, int TotalCount)> FilterByPriceAsync(
            int? minPrice, int? maxPrice, int page, int pageSize)
        {
            var q = AdminBaseQuery();
            if (minPrice.HasValue) q = q.Where(p => p.BasePrice >= minPrice.Value);
            if (maxPrice.HasValue) q = q.Where(p => p.BasePrice <= maxPrice.Value);
            return await PageAsync(q, page, pageSize);
        }

        // ── Filter: chỉ lọc theo nhiều nhãn hiệu ────────────────────────────────
        public async Task<(List<Product> products, int TotalCount)> FilterByBrandsAsync(
            string brandIds, int page, int pageSize)
        {
            var ids = brandIds.Split(',').Select(int.Parse).ToList();
            var q = AdminBaseQuery().Where(p => p.BrandId.HasValue && ids.Contains(p.BrandId.Value));
            return await PageAsync(q, page, pageSize);
        }

        // ── Filter: chỉ lọc theo nhiều danh mục ─────────────────────────────────
        public async Task<(List<Product> products, int TotalCount)> FilterByCategoriesAsync(
            string categoryIds, int page, int pageSize)
        {
            var ids = categoryIds.Split(',').Select(int.Parse).ToList();
            var q = AdminBaseQuery().Where(p => p.CategoryId.HasValue && ids.Contains(p.CategoryId.Value));
            return await PageAsync(q, page, pageSize);
        }

        // ── Filter: chỉ lọc theo trạng thái tồn kho ─────────────────────────────
        public async Task<(List<Product> products, int TotalCount)> FilterByStockAsync(
            string stockStatus, int page, int pageSize)
        {
            var q = AdminBaseQuery();
            q = stockStatus switch
            {
                "inStock"    => q.Where(p => p.ProductDetails.Sum(d => d.StockQuantity ?? 0) > 5),
                "lowStock"   => q.Where(p => p.ProductDetails.Sum(d => d.StockQuantity ?? 0) >= 1
                                          && p.ProductDetails.Sum(d => d.StockQuantity ?? 0) <= 5),
                "outOfStock" => q.Where(p => p.ProductDetails.Sum(d => d.StockQuantity ?? 0) == 0),
                _ => q
            };
            return await PageAsync(q, page, pageSize);
        }

        // ── Filter: chỉ lấy SP đang khuyến mãi ──────────────────────────────────
        public async Task<(List<Product> products, int TotalCount)> FilterByDiscountAsync(
            int page, int pageSize)
        {
            var q = AdminBaseQuery().Where(p => p.DiscountPrice != null && p.DiscountPrice > 0);
            return await PageAsync(q, page, pageSize);
        }

        // ── Filter: chỉ lọc theo đánh giá tối thiểu ─────────────────────────────
        public async Task<(List<Product> products, int TotalCount)> FilterByRatingAsync(
            int minRating, int page, int pageSize)
        {
            var minRat = (double)minRating;
            var q = AdminBaseQuery().Where(p =>
                p.ProductDetails.SelectMany(d => d.OrderDetails).Any(od => od.Review != null) &&
                p.ProductDetails.SelectMany(d => d.OrderDetails)
                    .Where(od => od.Review != null)
                    .Average(od => (double)od.Review!.Rating) >= minRat);
            return await PageAsync(q, page, pageSize);
        }

        // ── Sort: chỉ sắp xếp theo tiêu chí ─────────────────────────────────────
        public async Task<(List<Product> products, int TotalCount)> SortProductsAsync(
            string sortBy, bool sortDesc, int page, int pageSize)
        {
            var q = AdminBaseQuery();
            var count = await q.CountAsync();
            var ordered = sortBy switch
            {
                "price" => sortDesc ? q.OrderByDescending(p => p.BasePrice) : q.OrderBy(p => p.BasePrice),
                "name"  => sortDesc ? q.OrderByDescending(p => p.ProductName) : q.OrderBy(p => p.ProductName),
                "stock" => sortDesc ? q.OrderByDescending(p => p.ProductDetails.Sum(d => d.StockQuantity ?? 0))
                                    : q.OrderBy(p => p.ProductDetails.Sum(d => d.StockQuantity ?? 0)),
                "sold"  => sortDesc ? q.OrderByDescending(p => p.SoldQuantity ?? 0)
                                    : q.OrderBy(p => p.SoldQuantity ?? 0),
                _       => q.OrderByDescending(p => p.ProductId),
            };
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, count);
        }
    }
}