using System.Threading.Tasks;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<(List<Product> products, int TotalCount)> GetAll();
        Task<(List<Product> products, int TotalCount)> SearchAsync(string? keyword, decimal? minPrice, decimal? maxPrice, bool? Voucher, bool? isBestSeller, string? sortBy, int page, int pageSize);
        Task<List<Product>> GetProductsForHomePageAsync(List<int> categoryIds);
        Task<(List<Product> products, int TotalCount)> GetProductsByCategorySlugAsync(string categorySlug, int page, int pageSize);

        Task<Product?> GetProductDetailBySlugAsync(string slug);

    }
}
