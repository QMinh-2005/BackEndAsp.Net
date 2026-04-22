using System.Text.RegularExpressions;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Service
{
    public interface IProductService
    {
        Task<List<ProductHomeResponse>> GetProductsForHomePageAsync();
        Task<(List<Product> products, int TotalCount)> SeacrhAsync(string? key, string? categorySlug, string? brandSlug, decimal? minPrice, decimal? maxPrice, bool? Voucher, int page, int pageSize);
        string GenerateSlug(string title);
    }
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public async Task<List<ProductHomeResponse>> GetProductsForHomePageAsync()
        {
            List<int> categories = new List<int> { 1, 2, 7 };
            var products = await _productRepository.GetProductsForHomePageAsync(categories);
            var response = products.Select(p => new ProductHomeResponse
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Slug = p.Slug,
                MainImageUrl = p.MainImageUrl,
                CategoryName = p.Category.CategoryName,
                BasePrice = p.BasePrice,
                SellingPrice = (decimal)(p.DiscountPrice.HasValue ? p.DiscountPrice : p.BasePrice),
                DiscountPercent = p.DiscountPrice.HasValue && p.BasePrice > 0
                ? (int)Math.Round((p.BasePrice - p.DiscountPrice.Value) / p.BasePrice * 100)
                : 0,
                IsBestSeller = p.SoldQuantity >= 10
            }).ToList();
            return response;
        }
        public async Task<(List<Product> products, int TotalCount)> SeacrhAsync(string? keyword, string? categorySlug, string? brandSlug, decimal? minPrice, decimal? maxPrice, bool? Voucher, int page, int pageSize)
        {
            return await _productRepository.SearchAsync(keyword, categorySlug, brandSlug, minPrice, maxPrice, Voucher, page, pageSize);
        }
        public string GenerateSlug(string title)
        {
            if (string.IsNullOrEmpty(title)) return "";

            // Bước 1: Chuyển thành chữ thường
            string slug = title.ToLower();

            // Bước 2: Xóa dấu Tiếng Việt (bạn có thể dùng thư viện hoặc viết regex)
            // slug = RemoveVietnameseAccents(slug); 

            // Bước 3: Thay khoảng trắng và các ký tự đặc biệt bằng dấu gạch ngang
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');

            return slug;
        }
    }
}
