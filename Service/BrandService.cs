using Mapster;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Repositories;

namespace MyOwnLearning.Service
{
    public interface IBrandService
    {
        Task<(List<BrandResponse>, int TotalCount)> GetAllBrands();
        Task<Brand?> CreateBrandAsync(string brandName);
        Task<bool> DeleteBrandAsync(int brandId);
        Task<BrandResponse?> UpdateBrandAsync(int brandId, string newBrandName);
    }
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IProductService _productService;
        public BrandService(IBrandRepository brandRepository, IProductService productService)
        {
            _brandRepository = brandRepository;
            _productService = productService;
        }
        public async Task<(List<BrandResponse>, int TotalCount)> GetAllBrands()
        {
            var Brands = await _brandRepository.GetAllAsync();
            var res = Brands.Adapt<List<BrandResponse>>();
            var totalCount = res.Count();
            return (res, totalCount);
        }
        public async Task<Brand?> CreateBrandAsync(string brandName)
        {
            if (!string.IsNullOrWhiteSpace(brandName))
            {
                var newBrand = new Brand();
                newBrand.BrandName = brandName;
                newBrand.Slug = _productService.GenerateSlug("", brandName).ToLower();
                await _brandRepository.AddAsync(newBrand);
                return newBrand;
            }
            else return null;
        }
        public async Task<BrandResponse?> UpdateBrandAsync(int brandId, string newBrandName)
        {
            var brand = await _brandRepository.GetByIdAsync(brandId);
            if (brand != null && !string.IsNullOrWhiteSpace(newBrandName))
            {
                brand.BrandName = newBrandName;
                brand.Slug = _productService.GenerateSlug("", newBrandName).ToLower();
                await _brandRepository.UpdateAsync(brand);
                return brand.Adapt<BrandResponse>();
            }
            return null;
        }
        public async Task<bool> DeleteBrandAsync(int brandId)
        {
            var brand = await _brandRepository.GetByIdAsync(brandId);
            if (brand != null)
            {
                await _brandRepository.DeleteAsync(brandId);
                return true;
            }
            return false;
        }
    }
}
