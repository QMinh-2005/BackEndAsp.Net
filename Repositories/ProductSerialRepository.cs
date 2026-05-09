using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class ProductSerialRepository : Repository<ProductSerial>, IProductSerialRepository
    {
        public ProductSerialRepository(WebBadmintonContext context) : base(context)
        {
        }
        public async Task<bool> IsSerialNumberExistsAsync(string serialNumber)
        {
            return await _dbset.AnyAsync(ps => ps.SerialNumber == serialNumber.Trim());
        }
    }
}