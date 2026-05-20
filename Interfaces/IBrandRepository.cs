using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IBrandRepository : IRepository<Brand>
    {
        Task<int?> GetIdByBrandName(string brandName);
    }
}
