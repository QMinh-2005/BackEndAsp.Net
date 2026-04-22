using System.Threading.Tasks;
using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<(List<Product> products, int TotalCount)> GetAll();
        Task<(List<Product> products, int TotalCount)> SearchAsync(string? keyword, string? categorySlug, string? brandSlug, decimal? minPrice, decimal? maxPrice, bool? Voucher, int page, int pageSize);
        Task<List<Product>> GetProductsForHomePageAsync(List<int> categoryIds);
    }
}
