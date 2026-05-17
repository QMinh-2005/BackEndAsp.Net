using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IProductImageRepository : IRepository<ProductImage>
    {
        Task<List<ProductImage>> GetByProductIdAsync(int productId);
        Task<ProductImage?> GetMainImageByProductIdAsync(int productId);
        Task<bool> IsExistDisplayOrder(int displayOrder);
    }
}

