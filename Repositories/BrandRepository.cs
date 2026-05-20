using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class BrandRepository : Repository<Brand>, IBrandRepository
    {
        public BrandRepository(WebBadmintonContext context) : base(context) { }
        public async Task<int?> GetIdByBrandName(string brandName)
        {
            return await _dbset
                .Where(b => b.BrandName == brandName)
                .Select(b => (int?)b.BrandId) // Chỉ Select mỗi cột CategoryId
                .FirstOrDefaultAsync();
        }
    }
}
