using Mapster;
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
        [Permission("WAREHOUSE", "CREATE")]
        public async Task<IActionResult> CreateCategory([FromBody] string categoryName)
        {
            var newCategory = await _categoryService.CreateCategoryAsync(categoryName);
            var res = newCategory.Adapt<CategoryResponse>();
            if (res == null)
            {
                return BadRequest(new { Message = "Thông tin danh mục không hợp lệ" });
            }

            return Ok(new
            {
                Message = "Tạo danh mục thành công",
                data = res
            });
        }
        [HttpPut("{categoryId}")]
        [Permission("WAREHOUSE", "UPDATE")]
        public async Task<IActionResult> UpdateCategory(int categoryId, [FromBody] string newCategoryName)
        {
            var updatedCategory = await _categoryService.UpdateCategoryAsync(categoryId, newCategoryName);
            if (updatedCategory == null)
            {
                return BadRequest(new { Message = "Thông tin danh mục không hợp lệ hoặc danh mục không tồn tại" });
            }
            return Ok(new
            {
                Message = "Cập nhật danh mục thành công",
                data = updatedCategory
            });
        }
        [HttpDelete("{categoryId}")]
        [Permission("WAREHOUSE", "DELETE")]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var isDeleted = await _categoryService.DeleteCategoryAsync(categoryId);
            if (!isDeleted)
            {
                return NotFound(new { Message = "Danh mục không tồn tại hoặc không thể xóa do có sản phẩm liên quan" });
            }
            return Ok(new { Message = "Xóa danh mục thành công" });
        }
    }
}
