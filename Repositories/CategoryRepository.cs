using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(WebBadmintonContext context) : base(context) { }
    }
}
