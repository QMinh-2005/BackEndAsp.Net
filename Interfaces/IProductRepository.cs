using System.Threading.Tasks;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<(List<Product> products, int TotalCount)> GetAll();
        Task<bool> IsExistProduct(string productName);
        Task<(List<Product> products, int TotalCount)> SearchAsync(string? categorySlug, string? brandSlug, string? keyword, decimal? minPrice, decimal? maxPrice, bool? Voucher, bool? isBestSeller, string? sortBy, int page, int pageSize);
        Task<List<Product>> GetProductsForHomePageAsync(List<int> categoryIds);
        Task<(List<Product> products, int TotalCount)> GetProductsByCategorySlugAsync(string categorySlug, int page, int pageSize);

        Task<Product?> GetProductDetailBySlugAsync(string slug);

        Task<(List<ProductDetail> productDetails, int TotalCount)> GetProductDetailsByIdAsync(int productId, int page, int pageSize);
        Task<Product?> GetProductForDeletionAsync(int productId);

        // ── Admin: mỗi method chỉ xử lý đúng 1 tiêu chí ─────────────────────
        Task<(List<Product> products, int TotalCount)> GetProductsForAdminAsync(string? keyword, int? categoryId, int? brandId, int page, int pageSize);
        Task<(List<Product> products, int TotalCount)> FilterByPriceAsync(int? minPrice, int? maxPrice, int page, int pageSize);
        Task<(List<Product> products, int TotalCount)> FilterByBrandsAsync(string brandIds, int page, int pageSize);
        Task<(List<Product> products, int TotalCount)> FilterByCategoriesAsync(string categoryIds, int page, int pageSize);
        Task<(List<Product> products, int TotalCount)> FilterByStockAsync(string stockStatus, int page, int pageSize);
        Task<(List<Product> products, int TotalCount)> FilterByDiscountAsync(int page, int pageSize);
        Task<(List<Product> products, int TotalCount)> FilterByRatingAsync(int minRating, int page, int pageSize);
        Task<(List<Product> products, int TotalCount)> SortProductsAsync(string sortBy, bool sortDesc, int page, int pageSize);
    }
}
