using Mapster;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Interfaces;

namespace MyOwnLearning.Service
{
    public interface IBrandService
    {
        Task<(List<BrandResponse>, int TotalCount)> GetAllBrands();
    }
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepository;
        public BrandService(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }
        public async Task<(List<BrandResponse>, int TotalCount)> GetAllBrands()
        {
            var Brands = await _brandRepository.GetAllAsync();
            var res = Brands.Adapt<List<BrandResponse>>();
            var totalCount = res.Count();
            return (res, totalCount);
        }
    }
}
