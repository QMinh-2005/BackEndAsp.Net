using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IProductDetailRepository : IRepository<ProductDetail>
    {
        Task<ProductDetail> getProductDetailByIdAsync(int detailId);
        Task<ProductDetail> getProductDetailWithSerialNumberAsync(int detailId);
    }
}
