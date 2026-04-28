using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32.SafeHandles;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Repositories;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]

    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("searchAsync")]
        public async Task<IActionResult> SeacrhAsync(
            [FromQuery] string? keyword,
            [FromQuery] string? categorySlug, // Hứng tham số từ FE
            [FromQuery] string? brandSlug,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] bool? Voucher,
            int page = 1,
            int pagesize = 10
            )
        {

            var (products, totalCount) = await _productService.SearchAsync(keyword, categorySlug, brandSlug, minPrice, maxPrice, Voucher, page, pagesize);

            var response = products.Select(p => new ProductResponse
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Slug = p.Slug,
                MainImageUrl = p.MainImageUrl,
                BasePrice = p.BasePrice,
                SellingPrice = (decimal)(p.DiscountPrice.HasValue ? p.DiscountPrice : p.BasePrice),
                DiscountPercent = p.DiscountPrice.HasValue && p.BasePrice > 0
                    ? (int)Math.Round((p.BasePrice - p.DiscountPrice.Value) / p.BasePrice * 100)
                    : 0,
                IsBestSeller = p.SoldQuantity >= 10
            }).ToList();

            return Ok(new
            {
                items = response,
                totalCount = totalCount,
                page,
                pagesize,
                totalPages = (int)Math.Ceiling((double)totalCount / pagesize)
            });
        }

        [HttpGet("home")]
        public async Task<IActionResult> GetHomeProducts()
        {
            var result = await _productService.GetProductsForHomePageAsync();
            return Ok(new
            {
                Message = "Thành công",
                Data = result
            });
        }

        //Hàm tạo một sản phẩm => CÓ thể cho admin nhập tay trong hệ thống
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct(CreateProductRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                if (request.ProductDetailRequests == null || !request.ProductDetailRequests.Any())
                {
                    return BadRequest(new { message = "Sản phẩm phải có ít nhất một trường Detail" });
                }
                var createdProduct = await _productService.CreateProductAsync(request);
                return Ok(new
                {
                    message = "Tạo sản phẩm thành công!",
                    ProductId = createdProduct.ProductId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Không thể thêm sản phẩm. Lỗi hệ thống:" + ex.Message });
            }
        }

        //Hàm tạo nhiều sản phẩm => có thể nhập từ file vào
        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMultipleProducts(List<CreateProductRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    return BadRequest(new { Message = "Danh sách sản phẩm trống!" });
                }
                var createdProducts = await _productService.CreateMultipleProductAsync(requests);
                return Ok(new
                {
                    Message = $"Đã tạo thành công {createdProducts.Count} sản phẩm mới!",
                    CreatedProductIds = createdProducts.Select(p => p.ProductId).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Thêm thất bại. Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{idPro}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int idPro, UpdateProductRequest request)
        {
            try
            {
                // Gọi Service để xử lý logic "Giữ nguyên nếu rỗng"
                var updatedProduct = await _productService.UpdateProductAsync(idPro, request);

                // Nếu Service trả về null nghĩa là ID không tồn tại
                if (updatedProduct == null)
                {
                    return NotFound(new { Message = $"Không tìm thấy sản phẩm với ID = {idPro}" });
                }

                return Ok(new
                {
                    Message = "Cập nhật sản phẩm thành công!",
                    ProductId = updatedProduct.ProductId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi cập nhật: " + ex.Message });
            }
        }
    }
}
