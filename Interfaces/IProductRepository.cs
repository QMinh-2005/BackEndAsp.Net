using System.Threading.Tasks;
using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<(List<Product> products, int TotalCount)> GetAll();
        Task<(List<Product> products, int TotalCount)> SeacrhAsync(string? key, decimal? minPrice, decimal? maxPrice, bool? Voucher);
    }
}
