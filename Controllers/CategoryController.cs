using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Models;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategotyService _categoryService;
        public CategoryController(ICategotyService categoryService)
        {
            _categoryService = categoryService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllCategory()
        {
            var (categories, TotalCount) = await _categoryService.GetAllCategoryAsync();
            return Ok(new
            {
                data = categories,
                TotalCount
            });
        }
        [HttpPost]
        public async Task<IActionResult> CreateCategory(string categoryName)
        {
            var res = await _categoryService.CreateCategoryAsync(categoryName);
            if (res == null)
            {
                return BadRequest(new { Message = "Thông tin danh mục không hợp lệ" });
            }
            return Ok(new
            {
                Message = "Tạo danh mục thành công",
                CategoryId = res.CategoryId
            });
        }
    }
}
