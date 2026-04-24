using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class BrandRepository : Repository<Brand>, IBrandRepository
    {
        public BrandRepository(WebBadmintonContext context) : base(context) { }
    }
}
