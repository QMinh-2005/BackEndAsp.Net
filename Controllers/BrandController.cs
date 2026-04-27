using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandController : ControllerBase
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBrands()
        {
            var (Brands, TotalCount) = await _brandService.GetAllBrands();
            return Ok(new
            {
                data = Brands,
                TotalCount
            });
        }
        [HttpPost]
        public async Task<IActionResult> CreateBrand([FromBody] string brandName)
        {
            var newBrand = await _brandService.CreateBrandAsync(brandName);
            var res = newBrand.Adapt<BrandResponse>();
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
        [HttpPut("{brandId}")]
        [Permission("WAREHOUSE", "UPDATE")]
        public async Task<IActionResult> UpdateBrand(int brandId, [FromBody] string newBrandName)
        {
            var updatedBrand = await _brandService.UpdateBrandAsync(brandId, newBrandName);
            if (updatedBrand == null)
            {
                return BadRequest(new { Message = "Thông tin danh mục không hợp lệ hoặc danh mục không tồn tại" });
            }
            return Ok(new
            {
                Message = "Cập nhật danh mục thành công",
                data = updatedBrand
            });
        }

        [HttpDelete("{brandId}")]
        [Permission("WAREHOUSE", "DELETE")]
        public async Task<IActionResult> DeleteBrand(int brandId)
        {
            var isDeleted = await _brandService.DeleteBrandAsync(brandId);
            if (!isDeleted)
            {
                return BadRequest(new { Message = "Không thể xóa thương hiệu này vì có sản phẩm liên quan hoặc thương hiệu không tồn tại" });
            }
            return Ok(new
            {
                Message = "Xóa thương hiệu thành công"
            });
        }
    }
}
