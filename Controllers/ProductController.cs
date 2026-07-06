using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32.SafeHandles;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.DTO.Response.Admin;
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
        public async Task<IActionResult> SearchAll(
            [FromQuery] string? categorySlug,
            [FromQuery] string? brandSlug,
            [FromQuery] string? keyword,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] bool? Voucher,
            [FromQuery] bool? isBestSeller,
            [FromQuery] string? sortBy,
            int page = 1,
            int pagesize = 10
            )
        {

            var (products, totalCount) = await _productService.SearchAsync(categorySlug, brandSlug, keyword, minPrice, maxPrice, Voucher, isBestSeller, sortBy, page, pagesize);

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

        [HttpGet("product_of_category/{categorySlug}")]
        public async Task<IActionResult> GetProductsByCategorySlug(
            string categorySlug,
            int page = 1,
            int pagesize = 10)
        {
            var (products, totalCount) = await _productService.GetProductByCategorySlugAsync(categorySlug, page, pagesize);
            return Ok(new
            {
                items = products,
                totalCount = totalCount,
                page,
                pagesize,
                totalPages = (int)Math.Ceiling((double)totalCount / pagesize)
            });
        }
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetProductDetail(string slug)
        {
            try
            {
                var result = await _productService.GetProductDetailAsync(slug);

                if (result == null)
                    return NotFound(new { Message = "Không tìm thấy sản phẩm" });

                return Ok(new
                {
                    Message = "Thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ── Helper dùng chung cho tất cả admin filter endpoints ──────────────────
        private IActionResult AdminOk(List<ProductAdminResponse> products, int totalCount, int page, int pagesize)
        {
            var avgPrice = products.Count > 0
                ? products.Average(p => (double)(p.DiscountPrice.HasValue && p.DiscountPrice > 0
                    ? p.DiscountPrice.Value : p.BasePrice))
                : (double?)null;
            return Ok(new
            {
                Message = "Thành công",
                Items = products,
                TotalCount = totalCount,
                Page = page,
                PageSize = pagesize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pagesize),
                AveragePrice = avgPrice
            });
        }

        // ── Base: keyword + single category/brand ────────────────────────────────
        [HttpGet("product-management")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetProductsForManagement(
            [FromQuery] string? keyword, [FromQuery] int? categoryId, [FromQuery] int? brandId,
            int page = 1, int pagesize = 12)
        {
            try
            {
                var (products, totalCount) = await _productService.GetProductsForAdminAsync(keyword, categoryId, brandId, page, pagesize);
                return AdminOk(products, totalCount, page, pagesize);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // ── Filter: chỉ lọc theo khoảng giá ─────────────────────────────────────
        [HttpGet("product-management/filter-price")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FilterByPrice(
            [FromQuery] int? minPrice, [FromQuery] int? maxPrice,
            int page = 1, int pagesize = 12)
        {
            try
            {
                var (products, totalCount) = await _productService.FilterByPriceAsync(minPrice, maxPrice, page, pagesize);
                return AdminOk(products, totalCount, page, pagesize);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // ── Filter: chỉ lọc theo nhiều nhãn hiệu ────────────────────────────────
        [HttpGet("product-management/filter-brands")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FilterByBrands(
            [FromQuery] string brandIds, int page = 1, int pagesize = 12)
        {
            try
            {
                var (products, totalCount) = await _productService.FilterByBrandsAsync(brandIds, page, pagesize);
                return AdminOk(products, totalCount, page, pagesize);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // ── Filter: chỉ lọc theo nhiều danh mục ─────────────────────────────────
        [HttpGet("product-management/filter-categories")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FilterByCategories(
            [FromQuery] string categoryIds, int page = 1, int pagesize = 12)
        {
            try
            {
                var (products, totalCount) = await _productService.FilterByCategoriesAsync(categoryIds, page, pagesize);
                return AdminOk(products, totalCount, page, pagesize);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // ── Filter: chỉ lọc theo trạng thái tồn kho ─────────────────────────────
        [HttpGet("product-management/filter-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FilterByStock(
            [FromQuery] string stockStatus, int page = 1, int pagesize = 12)
        {
            try
            {
                var (products, totalCount) = await _productService.FilterByStockAsync(stockStatus, page, pagesize);
                return AdminOk(products, totalCount, page, pagesize);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // ── Filter: chỉ lấy SP đang khuyến mãi ──────────────────────────────────
        [HttpGet("product-management/filter-discount")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FilterByDiscount(int page = 1, int pagesize = 12)
        {
            try
            {
                var (products, totalCount) = await _productService.FilterByDiscountAsync(page, pagesize);
                return AdminOk(products, totalCount, page, pagesize);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // ── Filter: chỉ lọc theo đánh giá tối thiểu ─────────────────────────────
        [HttpGet("product-management/filter-rating")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FilterByRating(
            [FromQuery] int minRating, int page = 1, int pagesize = 12)
        {
            try
            {
                var (products, totalCount) = await _productService.FilterByRatingAsync(minRating, page, pagesize);
                return AdminOk(products, totalCount, page, pagesize);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // ── Sort: chỉ sắp xếp theo tiêu chí ─────────────────────────────────────
        [HttpGet("product-management/sort")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SortProducts(
            [FromQuery] string sortBy, [FromQuery] bool sortDesc = true,
            int page = 1, int pagesize = 12)
        {
            try
            {
                var (products, totalCount) = await _productService.SortProductsAsync(sortBy, sortDesc, page, pagesize);
                return AdminOk(products, totalCount, page, pagesize);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

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

        [HttpPut("{idPro}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int idPro, UpdateProductRequest request)
        {
            try
            {
                // Gọi Service để xử lý logic "Giữ nguyên nếu rỗng"
                var updatedProduct = await _productService.UpdateProductAsync(idPro, request);

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
                return StatusCode(500, new { Message = "Lỗi hệ thống khi cập nhật sản phẩm: " + ex.Message });
            }
        }

        [HttpDelete("{idPro}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int idPro)
        {
            try
            {
                var success = await _productService.DeleteProductAsync(idPro);
                if (!success)
                    return NotFound(new { Message = $"Không tìm thấy sản phẩm với ID = {idPro}" });
                return Ok(new { Message = "Xóa sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi xóa sản phẩm: " + ex.Message });
            }
        }


        // =====================================================================
        // API IMPORT SẢN PHẨM BẰNG EXCEL
        // =====================================================================
        [HttpPost("admin/import-excel")]
        [Authorize(Roles = "Admin")] // Bắt buộc phải là Admin
        [Consumes("multipart/form-data")] // Chỉ định API này nhận File
        public async Task<IActionResult> ImportProductsFromExcel(IFormFile file)
        {
            try
            {
                var result = await _productService.ImportBasicProductsFromExcelAsync(file);
                return Ok(new { message = result });
            }
            catch (ArgumentException ex)
            {
                // Bắt các lỗi do file trống, sai định dạng
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Bắt các lỗi trong quá trình đọc data (thiếu ID, sai kiểu...)
                return StatusCode(500, new { message = "Lỗi hệ thống khi Import: " + ex.Message });
            }
        }

        // =====================================================================
        // API EXPORT DANH SÁCH SẢN PHẨM RA EXCEL
        // =====================================================================
        [HttpGet("admin/export-excel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportProductsToExcel()
        {
            try
            {
                var excelBytes = await _productService.ExportProductsToExcelAsync();

                // Định dạng MIME type chuẩn cho file .xlsx
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = $"Products_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // Trả về file cho Frontend tải xuống
                return File(excelBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi Export: " + ex.Message });
            }
        }

        [HttpGet("{productId}/management-details")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetProductDetailsById(int productId, int page = 1, int pagesize = 10)
        {
            try
            {
                var (productDetails, totalCount) = await _productService.GetProductDetailsByIdAsync(productId, page, pagesize);
                if (productDetails == null || !productDetails.Any())
                    return NotFound(new { Message = "Không tìm thấy chi tiết sản phẩm nào" });
                return Ok(new
                {
                    Message = "Thành công",
                    Items = productDetails,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pagesize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pagesize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpPost("{productId}/management-details")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProductDetail(int productId, CreateProductDetailRequest request)
        {
            try
            {
                var newDetail = await _productService.AddVariantAsync(productId, request);
                return Ok(new
                {
                    Message = "Thêm chi tiết sản phẩm thành công!",
                    Detail = newDetail
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpPut("management-details/{detailId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVariant(int detailId, UpdateProductDetailRequest request)
        {
            try
            {
                var updatedDetail = await _productService.UpdateVariantAsync(detailId, request);
                if (updatedDetail == null)
                    return NotFound(new { Message = $"Không tìm thấy chi tiết sản phẩm với ID = {detailId}" });
                return Ok(new
                {
                    Message = "Cập nhật chi tiết sản phẩm thành công!",
                    Detail = updatedDetail
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi cập nhật chi tiết sản phẩm: " + ex.Message });
            }
        }
        [HttpDelete("management-details/{detailId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVariant(int detailId)
        {
            try
            {
                var success = await _productService.DeleteVariantAsync(detailId);
                if (!success)
                    return NotFound(new { Message = $"Không tìm thấy chi tiết sản phẩm với ID = {detailId}" });
                return Ok(new { Message = "Xóa chi tiết sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi xóa chi tiết sản phẩm: " + ex.Message });
            }
        }

        [HttpPost("{productId}/management-details/import-excel")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportProductDetailsFromExcel(int productId, IFormFile file)
        {
            try
            {
                var result = await _productService.ImportProductDetailsFromExcelAsync(productId, file);
                return Ok(new { message = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi Import chi tiết sản phẩm: " + ex.Message });
            }
        }

        [HttpGet("{productId}/management-details/export-excel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportProductDetailsToExcel(int productId)
        {
            try
            {
                var excelBytes = await _productService.ExportProductDetailsToExcelAsync(productId);

                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = $"ProductDetails_{productId}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi Export chi tiết sản phẩm: " + ex.Message });
            }
        }

        [HttpGet("management-details/{detailId}/serials")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetVariantSerials(int detailId, int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _productService.GetSerialNumbersByVariantIdAsync(detailId, page, pageSize);
                if (result == null)
                    return NotFound(new { Message = $"Không tìm thấy chi tiết sản phẩm với ID = {detailId}" });
                return Ok(new
                {
                    Message = "Thành công",
                    Data = result,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpPost("management-details/{detailId}/serials")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddVariantSerials(int detailId, CreateProductSerialRequest request)
        {
            try
            {
                var newSerials = await _productService.AddSingleSerialNumberAsync(detailId, request);
                return Ok(new
                {
                    Message = "Thêm số serial thành công!",
                    Serials = newSerials
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        //Quản lý ảnh
        [HttpGet("{productId}/management-images")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            try
            {
                var result = await _productService.GetProductImagesAsync(productId);
                return Ok(new { Message = "Thành công", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("{productId}/management-images")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddProductImage(int productId, IFormFile file, [FromForm] bool isMain = false)
        {
            try
            {
                var newImage = await _productService.AddProductImageAsync(productId, file, isMain);
                return Ok(new { Message = "Thêm ảnh vào bộ sưu tập thành công!", Data = newImage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{productId}/management-images/set-main/{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetMainImage(int productId, int imageId)
        {
            try
            {
                var success = await _productService.SetMainImageAsync(productId, imageId);
                if (!success) return NotFound(new { Message = "Không tìm thấy ảnh hoặc sản phẩm phù hợp." });
                return Ok(new { Message = "Thay đổi ảnh đại diện thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("management-images/reOrder")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReorderImages(int productId, [FromBody] List<UpdateImageOrderRequest> request)
        {
            try
            {
                await _productService.UpdateImagesOrderAsync(productId, request);
                return Ok(new { Message = "Cập nhật thứ tự hiển thị ảnh thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpDelete("management-images/{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductImage(int imageId)
        {
            try
            {
                var success = await _productService.DeleteImageAsync(imageId);
                if (!success) return NotFound(new { Message = "Không tìm thấy hình ảnh cần xóa." });
                return Ok(new { Message = "Xóa hình ảnh khỏi bộ sưu tập thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi xóa ảnh: " + ex.Message });
            }
        }
    }
}
