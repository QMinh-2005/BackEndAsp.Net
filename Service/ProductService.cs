using System.Text.RegularExpressions;
using Mapster;
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
        string GenerateSlug(string categorySlug, string title);
        Task<Product> CreateProductAsync(CreateProductRequest request);
        Task<List<Product>> CreateMultipleProductAsync(List<CreateProductRequest> requests);
        Task<Product> UpdateProductAsync(int idPro, UpdateProductRequest request);
    }
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;
        public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, IBrandRepository brandRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
        }

        private string RemoveVietnameseAccents(string text)
        {
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ", "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ", "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ", "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ", "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ", "ÍÌỊỈĨ",
                "đ", "Đ",
                "ýỳỵỷỹ", "ÝỲỴỶỸ"
            };
            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }
            return text;
        }

        // SỬA: Logic sinh Slug ghép nối CategorySlug và ProductName
        public string GenerateSlug(string categorySlug, string title)
        {
            if (string.IsNullOrEmpty(title)) return "";

            // Xóa dấu tiếng việt và chuyển thành chữ thường
            string formattedTitle = RemoveVietnameseAccents(title).ToLower();

            // Xóa ký tự đặc biệt, chỉ giữ lại chữ, số và khoảng trắng
            formattedTitle = Regex.Replace(formattedTitle, @"[^a-z0-9\s-]", "");

            // Thay khoảng trắng thành dấu gạch ngang và xóa gạch ngang dư thừa
            formattedTitle = Regex.Replace(formattedTitle, @"\s+", "-").Trim('-');

            // Ghép CategorySlug vào phía trước (nếu có)
            if (!string.IsNullOrEmpty(categorySlug))
            {
                return $"{categorySlug}-{formattedTitle}";
            }

            return formattedTitle;
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

        //add 1 sản phẩm
        public async Task<Product?> CreateProductAsync(CreateProductRequest request)
        {
            // 1. CHẠY SONG SONG 2 TRUY VẤN KIỂM TRA (Tối ưu hiệu năng). Không cần sử dụng await _brand ..... await cate....
            var brandTask = _brandRepository.GetByIdAsync(request.BrandId);
            var categoryTask = _categoryRepository.GetByIdAsync(request.CategoryId);

            await Task.WhenAll(brandTask, categoryTask);

            var checkBrand = brandTask.Result;
            var checkCategory = categoryTask.Result;

            // 2. SỬA LẠI LOGIC NGƯỢC VÀ NÉM EXCEPTION RÕ RÀNG
            if (checkBrand == null)
            {
                throw new Exception($"Thương hiệu với ID {request.BrandId} không tồn tại trong hệ thống.");
            }

            if (checkCategory == null)
            {
                throw new Exception($"Danh mục với ID {request.CategoryId} không tồn tại trong hệ thống.");
            }
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
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
                Slug = GenerateSlug(category.Slug, request.ProductName),

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
            // 1. TỐI ƯU HÓA KIỂM TRA (CHỈ GỌI DB 1 LẦN ĐỂ KIỂM TRA TẤT CẢ)

            // Lấy ra danh sách các CategoryID và BrandID độc nhất (Distinct) từ mảng gửi lên
            var uniqueCategoryIds = requests.Select(r => r.CategoryId).Distinct().ToList();
            var uniqueBrandIds = requests.Select(r => r.BrandId).Distinct().ToList();

            // Query xuống DB xem những ID nào thực sự tồn tại (Giả sử Repo của bạn có hàm GetAll hoặc bạn tự viết hàm check)
            // Ví dụ dùng cách lấy tất cả ID hợp lệ:
            var validCategoryIds = (await _categoryRepository.GetAllAsync())
                                    .Select(c => c.CategoryId).ToList();
            var validBrandIds = (await _brandRepository.GetAllAsync())
                                    .Select(b => b.BrandId).ToList();

            // Kiểm tra xem có ID nào gửi lên mà KHÔNG CÓ trong DB không
            var invalidCategories = uniqueCategoryIds.Except(validCategoryIds).ToList();
            if (invalidCategories.Any())
                throw new Exception($"Các Category ID sau không tồn tại: {string.Join(", ", invalidCategories)}");
            //Hàm dừng luôn khi ném ra lỗi

            var invalidBrands = uniqueBrandIds.Except(validBrandIds).ToList();
            if (invalidBrands.Any())
                throw new Exception($"Các Brand ID sau không tồn tại: {string.Join(", ", invalidBrands)}");
            var newPro = new List<Product>();
            foreach (var request in requests)
            {
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
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
                    Slug = GenerateSlug(category.Slug, request.ProductName),
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
        public async Task<Product?> UpdateProductAsync(int idPro, UpdateProductRequest request)
        {
            var pro = await _productRepository.GetByIdAsync(idPro);
            if (pro == null) return null;
            bool categoryChanged = request.CategoryId.HasValue && request.CategoryId.Value != pro.CategoryId;
            bool nameChanged = !string.IsNullOrWhiteSpace(request.ProductName) && request.ProductName != pro.ProductName;
            // 1. KIỂM TRA BRAND VÀ CATEGORY CÓ TỒN TẠI KHÔNG (NẾU CÓ TRUYỀN LÊN)

            var brandTask = request.BrandId.HasValue
        ? _brandRepository.GetByIdAsync(request.BrandId.Value)
        : Task.FromResult<Brand?>(null);

            var categoryTask = request.CategoryId.HasValue
                ? _categoryRepository.GetByIdAsync(request.CategoryId.Value)
                : Task.FromResult<Category?>(null);

            // Chờ cả 2 truy vấn chạy xong cùng lúc
            await Task.WhenAll(brandTask, categoryTask);

            // 2. XỬ LÝ KẾT QUẢ KIỂM TRA (Gán ID mới nếu hợp lệ)
            if (request.BrandId.HasValue)
            {
                if (brandTask.Result == null) throw new Exception($"Thương hiệu với ID {request.BrandId.Value} không tồn tại.");
                pro.BrandId = request.BrandId.Value;
            }
            if (request.CategoryId.HasValue)
            {
                if (brandTask.Result == null) throw new Exception($"Danh mục với ID {request.CategoryId.Value} không tồn tại.");
                pro.CategoryId = request.CategoryId.Value;
            }

            // Biến này dùng để lưu tạm Category (tránh phải gọi DB lần 2 để lấy Slug)
            Category? currentCategory = null;

            if (nameChanged)
            {
                pro.ProductName = request.ProductName!;
            }

            if (categoryChanged || nameChanged)
            {
                // Nếu ở trên chưa lấy Category, thì bây giờ mới phải gọi DB để lấy
                if (currentCategory == null && pro.CategoryId.HasValue)
                {
                    currentCategory = await _categoryRepository.GetByIdAsync(pro.CategoryId.Value);
                }

                // Cập nhật lại Slug kết hợp từ Slug danh mục và Tên sản phẩm
                if (currentCategory != null)
                {
                    pro.Slug = GenerateSlug(currentCategory.Slug, pro.ProductName);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Description)) pro.Description = request.Description;

            if (request.BasePrice.HasValue) pro.BasePrice = request.BasePrice.Value;
            if (request.DiscountPrice.HasValue) pro.DiscountPrice = request.DiscountPrice.Value;

            if (!string.IsNullOrWhiteSpace(request.MainImageUrl)) pro.MainImageUrl = request.MainImageUrl;

            if (request.ProductDetailRequests != null && request.ProductDetailRequests.Any())
            {
                foreach (var detailReq in request.ProductDetailRequests)
                {
                    // Bỏ qua nếu Frontend không gửi SerialNumber (Vì đây là khóa nhận diện)
                    if (string.IsNullOrWhiteSpace(detailReq.SerialNumber)) continue;

                    // Tìm xem SerialNumber này đã có trong Database của sản phẩm này chưa
                    var existingDetail = pro.ProductDetails.FirstOrDefault(d => d.SerialNumber == detailReq.SerialNumber);

                    if (existingDetail != null)
                    {
                        // TRƯỜNG HỢP 1: ĐÃ TỒN TẠI -> CẬP NHẬT (Giữ nguyên nếu rỗng)
                        if (!string.IsNullOrWhiteSpace(detailReq.WeightClass)) existingDetail.WeightClass = detailReq.WeightClass;
                        if (!string.IsNullOrWhiteSpace(detailReq.GripSize)) existingDetail.GripSize = detailReq.GripSize;
                        if (!string.IsNullOrWhiteSpace(detailReq.BalancePoint)) existingDetail.BalancePoint = detailReq.BalancePoint;
                        if (!string.IsNullOrWhiteSpace(detailReq.Stiffness)) existingDetail.Stiffness = detailReq.Stiffness;

                        if (detailReq.MaxTension.HasValue) existingDetail.MaxTension = detailReq.MaxTension.Value;
                        if (detailReq.Price.HasValue) existingDetail.Price = detailReq.Price.Value;
                        if (detailReq.StockQuantity.HasValue) existingDetail.StockQuantity = detailReq.StockQuantity.Value;
                    }
                    else
                    {
                        // TRƯỜNG HỢP 2: CHƯA TỒN TẠI -> THÊM MỚI VÀO SẢN PHẨM HIỆN TẠI
                        pro.ProductDetails.Add(new ProductDetail
                        {
                            SerialNumber = detailReq.SerialNumber,
                            WeightClass = detailReq.WeightClass, // Thêm mới thì có gì lưu nấy
                            GripSize = detailReq.GripSize,
                            BalancePoint = detailReq.BalancePoint,
                            Stiffness = detailReq.Stiffness,
                            MaxTension = detailReq.MaxTension,
                            // Nếu không nhập giá riêng thì lấy giá gốc của sản phẩm, không có số lượng thì gán 1
                            Price = detailReq.Price ?? pro.BasePrice,
                            StockQuantity = detailReq.StockQuantity ?? 1
                        });
                    }
                }
            }
            await _productRepository.UpdateAsync(pro);
            return pro;
        }
    }
}
