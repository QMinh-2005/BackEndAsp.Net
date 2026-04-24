using System.Text.RegularExpressions;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Service
{
    public interface IProductService
    {
        Task<List<ProductHomeResponse>> GetProductsForHomePageAsync();
        Task<(List<Product> products, int TotalCount)> SeacrhAsync(string? key, string? categorySlug, string? brandSlug, decimal? minPrice, decimal? maxPrice, bool? Voucher, int page, int pageSize);
        string GenerateSlug(string title);
        Task<Product> CreateProductAsync(CreateProductRequest request);
        Task<List<Product>> CreateMultipleProductAsync(List<CreateProductRequest> requests);
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
        //add 1 sản phẩm
        public async Task<Product> CreateProductAsync(CreateProductRequest request)
        {
            var newPro = new Product
            {
                ProductName = request.ProductName,
                BrandId = request.BrandId,
                CategoryId = request.CategoryId,
                Description = request.Description,
                BasePrice = request.BasePrice,
                MainImageUrl = request.MainImageUrl,
                DiscountPrice = request.DiscountPrice,
                SoldQuantity = 0, // Sản phẩm mới chưa bán được cái nào
                                  // Tự động sinh Slug từ tên sản phẩm
                Slug = GenerateSlug(request.ProductName),

                // 2. Chuyển đổi danh sách Detail DTO sang Entity ProductDetail
                ProductDetails = request.ProductDetailRequests.Select(d => new ProductDetail
                {
                    WeightClass = d.WeightClass,
                    GripSize = d.GripSize,
                    BalancePoint = d.BalancePoint,
                    Stiffness = d.Stiffness,
                    MaxTension = d.MaxTension,
                    Price = d.Price,
                    SerialNumber = d.SerialNumber,
                    StockQuantity = d.StockQuantity ?? 1,
                }).ToList()
            };
            await _productRepository.AddAsync(newPro);
            return newPro;
        }

        //add nhiều sản phẩm
        public async Task<List<Product>> CreateMultipleProductAsync(List<CreateProductRequest> requests)
        {
            var newPro = new List<Product>();
            foreach (var request in requests)
            {
                var pro = new Product
                {
                    ProductName = request.ProductName,
                    BrandId = request.BrandId,
                    CategoryId = request.CategoryId,
                    Description = request.Description,
                    BasePrice = request.BasePrice,
                    MainImageUrl = request.MainImageUrl,
                    DiscountPrice = request.DiscountPrice,
                    SoldQuantity = 0,
                    ProductDetails = request.ProductDetailRequests.Select(d => new ProductDetail
                    {
                        WeightClass = d.WeightClass,
                        GripSize = d.GripSize,
                        BalancePoint = d.BalancePoint,
                        Stiffness = d.Stiffness,
                        MaxTension = d.MaxTension,
                        Price = d.Price,
                        SerialNumber = d.SerialNumber,
                        StockQuantity = d.StockQuantity ?? 1
                    }).ToList()
                };
                newPro.Add(pro);
            }
            await _productRepository.AddRangeAsync(newPro);
            return newPro;
        }
    }
}
