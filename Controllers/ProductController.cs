using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Repositories;

namespace MyOwnLearning.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]

    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }


        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetAllProducts()
        {
            var res = await _productRepository.GetAll();
            return Ok(res.products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetByIdAsync(int id)
        {
            var res = await _productRepository.GetByIdAsync(id);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        //lấy sản phẩm có điều kiện lọc
        [HttpGet("searchAsync")]
        public async Task<IActionResult> SeacrhAsync(
            [FromQuery] string? keyword,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] bool? Voucher)
        {
            var res = await _productRepository.SeacrhAsync(keyword, minPrice, maxPrice, Voucher);
            return Ok(new { TotalItem = res.TotalCount, Data = res.products });
        }

        [HttpPost]
        public async Task<ActionResult<Product>> AddAsync(Product product)
        {
            var res = await _productRepository.AddAsync(product);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = res.ProductId }, res);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(int id, Product updatePro)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.ProductName = updatePro.ProductName;
            product.ProductDetails = updatePro.ProductDetails;
            product.Description = updatePro.Description;
            product.Category = updatePro.Category;
            product.Brand = updatePro.Brand;
            product.BrandId = updatePro.BrandId;
            product.BasePrice = updatePro.BasePrice;
            product.CategoryId = updatePro.CategoryId;
            product.VoucherConditions = updatePro.VoucherConditions;
            var res = await _productRepository.UpdateAsync(product);
            return Ok(res);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            await _productRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}
