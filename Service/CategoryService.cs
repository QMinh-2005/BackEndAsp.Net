using Mapster;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Service
{
    public interface ICategotyService
    {
        Task<(List<CategoryResponse>, int TotalCount)> GetAllCategoryAsync();
        Task<Category> CreateCategoryAsync(string categoryName);
    }
    public class CategoryService : ICategotyService
    {
        private readonly ICategoryRepository _categoryRepository;
        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }
        public async Task<(List<CategoryResponse>, int TotalCount)> GetAllCategoryAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var res = categories.Adapt<List<CategoryResponse>>();
            var totalCount = res.Count();
            return (res, totalCount);
        }
        public async Task<Category?> CreateCategoryAsync(string categoryName)
        {
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var newCate = new Category();
                newCate.CategoryName = categoryName;
                await _categoryRepository.AddAsync(newCate);
                return newCate;
            }
            else return null;

        }
    }
}
