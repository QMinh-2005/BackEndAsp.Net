using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Response.Admin;
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
                .Select(c => (int?)c.CategoryId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CategoryProductCountResponse>> GetProductCountPerCategoryAsync()
        {
            return await _dbset
                .Select(c => new CategoryProductCountResponse
                {
                    CategoryId   = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ProductCount = c.Products.Count()
                })
                .OrderByDescending(x => x.ProductCount)
                .ToListAsync();
        }
    }
}
