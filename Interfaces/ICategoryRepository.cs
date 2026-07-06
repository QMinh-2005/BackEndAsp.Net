using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<int?> GetIdByCategoryName(string categoryName);
        Task<List<CategoryProductCountResponse>> GetProductCountPerCategoryAsync();
    }
}
