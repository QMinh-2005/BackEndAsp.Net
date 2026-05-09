using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IProductSerialRepository : IRepository<ProductSerial>
    {
        Task<bool> IsSerialNumberExistsAsync(string serialNumber);
    }
}
