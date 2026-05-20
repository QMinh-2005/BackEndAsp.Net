using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(WebBadmintonContext context) : base(context) { }
        public async Task<int?> GetIdByCategoryName(string categoryName)
        {
            return await _dbset
                .Where(c => c.CategoryName == categoryName)
                .Select(c => (int?)c.CategoryId) // Chỉ Select mỗi cột CategoryId
                .FirstOrDefaultAsync();
        }
    }
}
