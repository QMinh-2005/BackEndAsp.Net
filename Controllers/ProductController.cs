using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Response;
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

            var (products, totalCount) = await _productService.SeacrhAsync(keyword, categorySlug, brandSlug, minPrice, maxPrice, Voucher, page, pagesize);

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
    }
}
