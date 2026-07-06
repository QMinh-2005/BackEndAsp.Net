using System.Text.RegularExpressions;
using Mapster;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Enums;
using MyOwnLearning.Helpers;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace MyOwnLearning.Service
{
    public interface IProductService
    {
        Task<List<ProductHomeResponse>> GetProductsForHomePageAsync();
        Task<(List<Product> products, int TotalCount)> SearchAsync(string? categorySlug, string? brandSlug, string? key, decimal? minPrice, decimal? maxPrice, bool? Voucher, bool? isBestSeller, string? sortBy, int page, int pageSize);
        string GenerateSlug(string categorySlug, string title);
        Task<(List<ProductResponse> products, int TotalCount)> GetProductByCategorySlugAsync(string categorySlug, int page, int pageSize);
        Task<ProductDetailResponse?> GetProductDetailAsync(string slug);

        //Trang 1 — mỗi method chỉ xử lý đúng 1 tiêu chí
        Task<(List<ProductAdminResponse> products, int TotalCount)> GetProductsForAdminAsync(string? keyword, int? categoryId, int? brandId, int page, int pageSize);
        Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByPriceAsync(int? minPrice, int? maxPrice, int page, int pageSize);
        Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByBrandsAsync(string brandIds, int page, int pageSize);
        Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByCategoriesAsync(string categoryIds, int page, int pageSize);
        Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByStockAsync(string stockStatus, int page, int pageSize);
        Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByDiscountAsync(int page, int pageSize);
        Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByRatingAsync(int minRating, int page, int pageSize);
        Task<(List<ProductAdminResponse> products, int TotalCount)> SortProductsAsync(string sortBy, bool sortDesc, int page, int pageSize);
        Task<Product> CreateProductAsync(CreateProductRequest request);
        Task<Product> UpdateProductAsync(int idPro, UpdateProductRequest request);
        Task<bool> DeleteProductAsync(int productId);
        Task<string> ImportBasicProductsFromExcelAsync(IFormFile file);
        Task<byte[]> ExportProductsToExcelAsync();


        //Trang 2

        Task<(List<ProductDetailAdminRespones> productDetails, int TotalCount)> GetProductDetailsByIdAsync(int productId, int page, int pageSize);
        Task<ProductDetailAdminRespones> AddVariantAsync(int productId, CreateProductDetailRequest request);
        Task<ProductDetailAdminRespones> UpdateVariantAsync(int productDetailId, UpdateProductDetailRequest request);
        Task<bool> DeleteVariantAsync(int productDetailId);
        Task<string> ImportProductDetailsFromExcelAsync(int productId, IFormFile file);
        Task<byte[]> ExportProductDetailsToExcelAsync(int productId);

        //Trang 3: Các phương thức liên quan đến quản lý SerialNumber sẽ được thêm sau khi hoàn thành phần quản lý Variant, vì SerialNumber phụ thuộc vào Variant (ProductDetail)
        Task<VariantSerialsResponse> GetSerialNumbersByVariantIdAsync(int productDetailId, int page, int pageSize);
        Task<SerialNumberDto> AddSingleSerialNumberAsync(int productDetailId, CreateProductSerialRequest request);

        //Quản lý Image
        Task<List<ProductImageResponse>> GetProductImagesAsync(int productId);
        Task<ProductImageResponse> AddProductImageAsync(int productId, IFormFile file, bool isMain);
        Task<bool> SetMainImageAsync(int productId, int imageId);
        Task<bool> UpdateImagesOrderAsync(int productId, List<UpdateImageOrderRequest> requests);
        Task<bool> DeleteImageAsync(int imageId);
    }
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductDetailRepository _productDetailRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IProductSerialRepository _productSerialRepository;
        private readonly IProductImageRepository _productImageRepository;
        private readonly IWebHostEnvironment _env;

        public ProductService(IProductRepository productRepository, IProductDetailRepository productDetailRepository, ICategoryRepository categoryRepository, IBrandRepository brandRepository, IProductSerialRepository productSerialRepository, IProductImageRepository productImageRepository, IWebHostEnvironment env)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _productSerialRepository = productSerialRepository;
            _productDetailRepository = productDetailRepository;
            _productImageRepository = productImageRepository;
            _env = env;
        }

        public string NormalizeProductName(string categoryName, string inputProductName)
        {
            if (string.IsNullOrWhiteSpace(inputProductName)) return string.Empty;
            if (string.IsNullOrWhiteSpace(categoryName)) return CapitalizeFirstLetter(inputProductName.Trim());

            string feName = inputProductName.Trim();
            string catName = categoryName.Trim();

            // Kịch bản 1: FE đã nhập chuẩn hoặc gần chuẩn toàn bộ (VD: "Vợt cầu lông Yonex", "vợt cầu lông yonex")
            // StringComparison.OrdinalIgnoreCase tự động bỏ qua khác biệt HOA/thường
            if (feName.StartsWith(catName, StringComparison.OrdinalIgnoreCase))
            {
                return CapitalizeFirstLetter(feName);
            }

            // Lấy từ đầu tiên của tên Danh mục (VD: chữ "Vợt" trong "Vợt cầu lông")
            string firstWordOfCat = catName.Split(' ')[0];

            // Kịch bản 2: FE nhập bị lặp từ đầu tiên nhưng sai kiểu (VD: "vợt Yonex Astrox", "VỢT lining")
            if (feName.StartsWith(firstWordOfCat, StringComparison.OrdinalIgnoreCase))
            {
                // Cắt bỏ phần bị lặp đi, chỉ lấy phần đuôi (Substring dựa trên độ dài của từ đầu tiên)
                string remainingName = feName.Substring(firstWordOfCat.Length).Trim();

                // Ghép tên Danh mục chuẩn trong DB với phần đuôi
                return CapitalizeFirstLetter($"{catName} {remainingName}");
            }

            // Kịch bản 3: FE chỉ nhập đúng tên model (VD: "Astrox 100zz" hoặc "Halbertec 8000")
            return CapitalizeFirstLetter($"{catName} {feName}");
        }

        // Hàm phụ trợ: Giúp viết hoa chữ cái đầu tiên của sản phẩm cho đẹp
        private string CapitalizeFirstLetter(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            if (text.Length == 1) return text.ToUpper();
            return char.ToUpper(text[0]) + text.Substring(1);
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
        public async Task<(List<Product> products, int TotalCount)> SearchAsync(string? categorySlug, string? brandSlug, string? keyword, decimal? minPrice, decimal? maxPrice, bool? Voucher, bool? isBestSeller, string? sortBy, int page, int pageSize)
        {
            return await _productRepository.SearchAsync(categorySlug, brandSlug, keyword, minPrice, maxPrice, Voucher, isBestSeller, sortBy, page, pageSize);
        }

        // ── Helper mapping dùng chung ─────────────────────────────────────────────
        private static List<ProductAdminResponse> MapToAdminResponse(List<Product> products) =>
            products.Select(p => new ProductAdminResponse
            {
                ProductId       = p.ProductId,
                ProductName     = p.ProductName,
                MainImageUrl    = p.MainImageUrl,
                BasePrice       = p.BasePrice,
                DiscountPrice   = p.DiscountPrice,
                delta = (p.BasePrice - p.DiscountPrice),
                DiscountPercent = p.DiscountPrice.HasValue && p.BasePrice > 0
                    ? (int)Math.Round((p.BasePrice - p.DiscountPrice.Value) / p.BasePrice * 100) : 0,
                BrandName    = p.Brand?.BrandName ?? "N/A",
                CategoryName = p.Category?.CategoryName ?? "N/A",
                VariantsCount = p.ProductDetails?.Count ?? 0,
                TotalStock    = p.ProductDetails?.Sum(d => d.StockQuantity ?? 0) ?? 0,
                SoldQuantity  = p.SoldQuantity ?? 0,
            }).OrderBy(p => p.delta).ToList();

        // ── Base search ───────────────────────────────────────────────────────────
        public async Task<(List<ProductAdminResponse> products, int TotalCount)> GetProductsForAdminAsync(
            string? keyword, int? categoryId, int? brand, int page, int pageSize)
        {
            var (products, totalCount) = await _productRepository.GetProductsForAdminAsync(keyword, categoryId, brand, page, pageSize);
            var response = MapToAdminResponse(products);
            return (response, totalCount);
        }

        // ── Filter: chỉ lọc theo khoảng giá ─────────────────────────────────────
        public async Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByPriceAsync(int? minPrice, int? maxPrice, int page, int pageSize)
        {
            var (products, count) = await _productRepository.FilterByPriceAsync(minPrice, maxPrice, page, pageSize);
            return (MapToAdminResponse(products), count);
        }

        // ── Filter: chỉ lọc theo nhiều nhãn hiệu ────────────────────────────────
        public async Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByBrandsAsync(string brandIds, int page, int pageSize)
        {
            var (products, count) = await _productRepository.FilterByBrandsAsync(brandIds, page, pageSize);
            return (MapToAdminResponse(products), count);
        }

        // ── Filter: chỉ lọc theo nhiều danh mục ─────────────────────────────────
        public async Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByCategoriesAsync(string categoryIds, int page, int pageSize)
        {
            var (products, count) = await _productRepository.FilterByCategoriesAsync(categoryIds, page, pageSize);
            return (MapToAdminResponse(products), count);
        }

        // ── Filter: chỉ lọc theo trạng thái tồn kho ─────────────────────────────
        public async Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByStockAsync(string stockStatus, int page, int pageSize)
        {
            var (products, count) = await _productRepository.FilterByStockAsync(stockStatus, page, pageSize);
            return (MapToAdminResponse(products), count);
        }

        // ── Filter: chỉ lấy SP đang khuyến mãi ──────────────────────────────────
        public async Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByDiscountAsync(int page, int pageSize)
        {
            var (products, count) = await _productRepository.FilterByDiscountAsync(page, pageSize);
            return (MapToAdminResponse(products), count);
        }

        // ── Filter: chỉ lọc theo đánh giá tối thiểu ─────────────────────────────
        public async Task<(List<ProductAdminResponse> products, int TotalCount)> FilterByRatingAsync(int minRating, int page, int pageSize)
        {
            var (products, count) = await _productRepository.FilterByRatingAsync(minRating, page, pageSize);
            return (MapToAdminResponse(products), count);
        }

        // ── Sort: chỉ sắp xếp ────────────────────────────────────────────────────
        public async Task<(List<ProductAdminResponse> products, int TotalCount)> SortProductsAsync(string sortBy, bool sortDesc, int page, int pageSize)
        {
            var (products, count) = await _productRepository.SortProductsAsync(sortBy, sortDesc, page, pageSize);
            return (MapToAdminResponse(products), count);
        }

        public async Task<Product?> CreateProductAsync(CreateProductRequest request)
        {
            var checkBrand = await _brandRepository.GetByIdAsync(request.BrandId);
            if (checkBrand == null)
            {
                throw new Exception($"Thương hiệu với ID {request.BrandId} không tồn tại trong hệ thống.");
            }

            var checkCategory = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (checkCategory == null)
            {
                throw new Exception($"Danh mục với ID {request.CategoryId} không tồn tại trong hệ thống.");
            }

            string finalProductName = NormalizeProductName(checkCategory.CategoryName ?? "", request.ProductName);

            string generatedSlug = GenerateSlug("", finalProductName);

            var existingProduct = await _productRepository.GetProductDetailBySlugAsync(generatedSlug);
            if (existingProduct != null)
                throw new Exception($"Sản phẩm '{finalProductName}' đã tồn tại trong hệ thống!");
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            var newPro = new Product
            {
                ProductName = finalProductName,
                BrandId = request.BrandId,
                CategoryId = request.CategoryId,
                Description = request.Description,
                BasePrice = request.BasePrice,
                MainImageUrl = request.MainImageUrl,
                DiscountPrice = request.DiscountPrice,
                SoldQuantity = 0, // Sản phẩm mới chưa bán được cái nào
                                  // Tự động sinh Slug từ tên sản phẩm
                Slug = generatedSlug,
            };
            await _productRepository.AddAsync(newPro);
            return newPro;
        }
        public async Task<Product?> UpdateProductAsync(int idPro, UpdateProductRequest request)
        {
            // 1. LẤY SẢN PHẨM (Lưu ý: Repository cần Include ProductDetails và ProductSerials)
            var pro = await _productRepository.GetByIdAsync(idPro);
            if (pro == null) return null;

            bool categoryChanged = request.CategoryId.HasValue && request.CategoryId.Value != pro.CategoryId;
            bool nameChanged = !string.IsNullOrWhiteSpace(request.ProductName) && request.ProductName != pro.ProductName;

            if (request.BrandId.HasValue)
            {
                var brand = await _brandRepository.GetByIdAsync(request.BrandId.Value);
                if (brand == null) throw new Exception($"Thương hiệu ID {request.BrandId.Value} không tồn tại.");
                pro.BrandId = request.BrandId.Value;
            }

            Category? currentCategory = null;
            if (request.CategoryId.HasValue)
            {
                currentCategory = await _categoryRepository.GetByIdAsync(request.CategoryId.Value);
                if (currentCategory == null) throw new Exception($"Danh mục ID {request.CategoryId.Value} không tồn tại.");
                pro.CategoryId = request.CategoryId.Value;
            }

            if (nameChanged || categoryChanged)
            {
                if (currentCategory == null && pro.CategoryId.HasValue)
                {
                    currentCategory = await _categoryRepository.GetByIdAsync(pro.CategoryId.Value);
                }

                if (currentCategory != null)
                {
                    // Chuẩn hóa lại tên (VD: Nếu đổi từ Balo sang Vợt, tên sẽ được gắn tiền tố mới)
                    string inputName = nameChanged ? request.ProductName! : pro.ProductName;
                    pro.ProductName = NormalizeProductName(currentCategory.CategoryName ?? "", inputName);

                    // Sinh lại Slug mới dựa trên tên đã chuẩn hóa
                    pro.Slug = GenerateSlug("", pro.ProductName);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Description)) pro.Description = request.Description;
            if (request.BasePrice.HasValue) pro.BasePrice = request.BasePrice.Value;
            if (request.DiscountPrice.HasValue) pro.DiscountPrice = request.DiscountPrice.Value;
            if (!string.IsNullOrWhiteSpace(request.MainImageUrl)) pro.MainImageUrl = request.MainImageUrl;
            await _productRepository.UpdateAsync(pro);
            return pro;
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await _productRepository.GetProductForDeletionAsync(productId);
            if (product == null) return false;
            bool hasSoldProducts = product.ProductDetails.Any(d => d.ProductSerials.Any(s => s.Status == ProductSerialStatus.Sold || s.Status == ProductSerialStatus.Reserved));
            if (hasSoldProducts)
                throw new Exception("Không thể xóa sản phẩm vì đã có đơn hàng liên quan. Vui lòng kiểm tra lại.");
            await _productRepository.DeleteAsync(productId);
            return true;
        }

        public async Task<string> ImportBasicProductsFromExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không được để trống.");

            if (!file.FileName.EndsWith(".xlsx"))
                throw new ArgumentException("Chỉ hỗ trợ file định dạng .xlsx");

            var productsToSave = new List<Product>();
            var errorMessages = new List<string>(); // Danh sách chứa các lỗi để thông báo

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    // Bắt đầu đọc từ dòng 2 (Vì dòng 1 là Header theo đúng file Export của bạn)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            // Cột 1 là Product ID -> Bỏ qua khi tạo mới
                            // Cột 2 là Tên Sản Phẩm
                            var productName = worksheet.Cells[row, 1].Text.Trim();
                            if (string.IsNullOrEmpty(productName)) continue;
                            if (await _productRepository.IsExistProduct(productName))
                            {
                                errorMessages.Add($"Sản phẩm với tên {productName} này đã tồn tại rồi");
                                continue;
                            }
                            // Cột 3 là Tên Danh Mục
                            string categoryName = worksheet.Cells[row, 2].Text.Trim();
                            // Cột 4 là Tên Thương Hiệu
                            string brandName = worksheet.Cells[row, 3].Text.Trim();

                            // Dò ID
                            var categoryId = await _categoryRepository.GetIdByCategoryName(categoryName);
                            var brandId = await _brandRepository.GetIdByBrandName(brandName);

                            if (!categoryId.HasValue)
                            {
                                errorMessages.Add($"Dòng {row}: Không tìm thấy danh mục '{categoryName}'");
                                continue; // Lỗi thì bỏ qua dòng này, chạy tiếp dòng sau
                            }
                            if (!brandId.HasValue)
                            {
                                errorMessages.Add($"Dòng {row}: Không tìm thấy thương hiệu '{brandName}'");
                                continue;
                            }

                            var product = new Product
                            {
                                ProductName = productName,
                                CategoryId = categoryId.Value,
                                BrandId = brandId.Value,
                                Description = worksheet.Cells[row, 4].Text.Trim(), // Cột 5: Mô Tả
                                BasePrice = decimal.TryParse(worksheet.Cells[row, 5].Text, out var bp) ? bp : 0, // Cột 6: Giá cơ bản
                                DiscountPrice = decimal.TryParse(worksheet.Cells[row, 6].Text, out var dpPrice) ? dpPrice : (decimal?)null, // Cột 7: Giá giảm
                                MainImageUrl = worksheet.Cells[row, 7].Text.Trim(), // Cột 8: Link ảnh
                                                                                    // Cột 9: Đã bán -> Bỏ qua (Set = 0 cho SP mới)
                                                                                    // Cột 10: Slug -> Tự động sinh
                                Slug = GenerateSlug("", productName),
                                SoldQuantity = 0
                            };

                            productsToSave.Add(product);
                        }
                        catch (Exception ex)
                        {
                            errorMessages.Add($"Dòng {row}: Lỗi định dạng dữ liệu ({ex.Message})");
                        }
                    }
                }
            }

            // Tiến hành lưu các sản phẩm ĐÚNG vào DB
            if (productsToSave.Any())
            {
                await _productRepository.AddRangeAsync(productsToSave);
            }

            // TỔNG HỢP CÂU TRẢ LỜI CHO FE
            string resultMessage = $"Đã import thành công {productsToSave.Count} sản phẩm.";
            if (errorMessages.Any())
            {
                resultMessage += $"\nTuy nhiên, có {errorMessages.Count} sản phẩm bị bỏ qua do lỗi:\n";
                resultMessage += string.Join("\n", errorMessages);
            }

            return resultMessage;
        }

        public async Task<byte[]> ExportProductsToExcelAsync()
        {
            // Lấy toàn bộ sản phẩm (Nên include Category và Brand để lấy tên)
            var (products, _) = await _productRepository.GetAll();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Products_Export");

                // Ghi Tiêu đề (Headers)
                string[] headers = {
            "Product ID", "Tên Sản Phẩm","Tên Danh Mục", "Tên Thương Hiệu", "Mô Tả",
            "Giá Cơ Bản", "Giá Giảm",
             "Link Ảnh Chính",
            "Đã Bán", "Slug"
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Đổ dữ liệu từ Database ra Excel
                int row = 2;
                foreach (var p in products)
                {
                    worksheet.Cells[row, 1].Value = p.ProductId;
                    worksheet.Cells[row, 2].Value = p.ProductName;
                    worksheet.Cells[row, 3].Value = p.Category?.CategoryName;
                    worksheet.Cells[row, 4].Value = p.Brand?.BrandName;
                    worksheet.Cells[row, 5].Value = p.Description;
                    worksheet.Cells[row, 6].Value = p.BasePrice;
                    worksheet.Cells[row, 7].Value = p.DiscountPrice;
                    worksheet.Cells[row, 8].Value = p.MainImageUrl;
                    worksheet.Cells[row, 9].Value = p.SoldQuantity;
                    worksheet.Cells[row, 10].Value = p.Slug;
                    row++;
                }

                worksheet.Cells.AutoFitColumns(); // Tự động căn chỉnh độ rộng cột
                return package.GetAsByteArray();
            }
        }
        public async Task<(List<ProductResponse> products, int TotalCount)> GetProductByCategorySlugAsync(string categorySlug, int page, int pageSize)
        {
            var (products, totalCount) = await _productRepository.GetProductsByCategorySlugAsync(categorySlug, page, pageSize);
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
            return (response, totalCount);
        }
        public async Task<ProductDetailResponse?> GetProductDetailAsync(string slug)
        {
            var product = await _productRepository.GetProductDetailBySlugAsync(slug);
            if (product == null) return null;

            var variants = product.ProductDetails?
                .Select(d => new ProductVariant
                {
                    DetailId = d.DetailId,
                    WeightClass = d.WeightClass,
                    GripSize = d.GripSize,
                    BalancePoint = d.BalancePoint,
                    Stiffness = d.Stiffness,
                    MaxTension = d.MaxTension,
                    Price = d.Price,
                    StockQuantity = d.StockQuantity ?? 0,

                    // Trả về true nếu số lượng > 0
                    InStock = (d.StockQuantity ?? 0) > 0
                }).ToList() ?? new List<ProductVariant>();

            return new ProductDetailResponse
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                BasePrice = product.BasePrice,
                SellingPrice = product.DiscountPrice ?? product.BasePrice,
                DiscountPercent = product.DiscountPrice.HasValue && product.BasePrice > 0
                    ? (int)Math.Round((product.BasePrice - product.DiscountPrice.Value) / product.BasePrice * 100)
                    : 0,
                MainImageUrl = product.MainImageUrl,
                Description = product.Description,

                // Sản phẩm được coi là "Còn hàng" nếu CÓ ÍT NHẤT 1 phân loại (Variant) có Stock > 0
                IsAvailable = variants.Any(v => v.InStock),

                // Map danh sách ảnh
                Imgaes = product.ProductImages?
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ProductImage
                    {
                        ImageUrl = i.ImageUrl,
                        DisplayOrder = i.DisplayOrder
                    }).ToList() ?? new List<ProductImage>(),

                Variants = variants
            };
        }

        public async Task<(List<ProductDetailAdminRespones> productDetails, int TotalCount)> GetProductDetailsByIdAsync(int productId, int page, int pageSize)
        {
            var (productDetails, totalCount) = await _productRepository.GetProductDetailsByIdAsync(productId, page, pageSize);
            var response = productDetails.Select(d => new ProductDetailAdminRespones
            {
                DetailId = d.DetailId,
                WeightClass = d.WeightClass,
                GripSize = d.GripSize,
                BalancePoint = d.BalancePoint,
                Stiffness = d.Stiffness,
                MaxTension = d.MaxTension,
                Price = d.Price,
                StockQuantity = d.StockQuantity,
                TotalSerialNumbers = d.ProductSerials?.Count ?? 0
            }).ToList();
            return (response, totalCount);
        }

        public async Task<ProductDetailAdminRespones> AddVariantAsync(int productId, CreateProductDetailRequest request)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) throw new Exception($"Sản phẩm với ID {productId} không tồn tại.");
            string? validWeightClass = VariantValidationHelper.ValidateAndMapStringAttribute(request.WeightClass, VariantAttributes.WeightClasses);
            string? validGripSize = VariantValidationHelper.ValidateAndMapStringAttribute(request.GripSize, VariantAttributes.GripSizes);
            string? validBalancePoint = VariantValidationHelper.ValidateAndMapStringAttribute(request.BalancePoint, VariantAttributes.BalancePoints);
            string? validStiffness = VariantValidationHelper.ValidateAndMapStringAttribute(request.Stiffness, VariantAttributes.Stiffness);
            int? validMaxTension = VariantValidationHelper.ValidateAndMapMaxTension(request.MaxTension);
            var (existingVariant, _) = await _productRepository.GetProductDetailsByIdAsync(productId, 1, int.MaxValue);
            bool isDuplicate = existingVariant.Any(v =>
                v.WeightClass == validWeightClass &&
                v.GripSize == validGripSize &&
                v.BalancePoint == validBalancePoint &&
                v.Stiffness == validStiffness &&
                v.MaxTension == validMaxTension);
            if (isDuplicate)
                throw new Exception("Variant đã tồn tại.");
            var newVariant = new ProductDetail
            {
                ProductId = productId,
                WeightClass = validWeightClass,
                GripSize = validGripSize,
                BalancePoint = validBalancePoint,
                Stiffness = validStiffness,
                MaxTension = validMaxTension,
                Price = request.Price > 0 ? request.Price : throw new Exception("Giá trị Price không hợp lệ."),
                StockQuantity = request.StockQuantity,
                ProductSerials = new List<ProductSerial>() // Khởi tạo danh sách Serial rỗng cho Variant mới
            };
            await _productDetailRepository.AddAsync(newVariant);

            var serialNumbers = new List<ProductSerial>();
            for (int i = 0; i < request.StockQuantity; i++)
            {
                string randomString = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                // Tương đương 'SN-' + CAST(pd.DetailID) + '-' + CAST(Nums.n) + '-' + UPPER(LEFT(NEWID(), 6))
                string generatedSerial = $"SN-{newVariant.DetailId}-{i}-{randomString}";
                serialNumbers.Add(new ProductSerial
                {
                    DetailId = newVariant.DetailId,
                    SerialNumber = generatedSerial,
                    Status = ProductSerialStatus.InStock,
                    ImportDate = DateTime.UtcNow
                });
            }
            newVariant.ProductSerials = serialNumbers;
            await _productDetailRepository.UpdateAsync(newVariant);
            var res = newVariant.Adapt<ProductDetailAdminRespones>();
            res.TotalSerialNumbers = serialNumbers.Count;
            return res;
        }
        public async Task<ProductDetailAdminRespones> UpdateVariantAsync(int productDetailId, UpdateProductDetailRequest request)
        {
            var variant = await _productDetailRepository.getProductDetailByIdAsync(productDetailId);
            if (variant == null) throw new Exception($"Variant với ID {productDetailId} không tồn tại.");
            variant.WeightClass = VariantValidationHelper.ValidateAndMapStringAttribute(request.WeightClass, VariantAttributes.WeightClasses);
            variant.GripSize = VariantValidationHelper.ValidateAndMapStringAttribute(request.GripSize, VariantAttributes.GripSizes);
            variant.BalancePoint = VariantValidationHelper.ValidateAndMapStringAttribute(request.BalancePoint, VariantAttributes.BalancePoints);
            variant.Stiffness = VariantValidationHelper.ValidateAndMapStringAttribute(request.Stiffness, VariantAttributes.Stiffness);
            variant.MaxTension = VariantValidationHelper.ValidateAndMapMaxTension(request.MaxTension);
            variant.Price = request.Price ?? variant.Price;

            var currentStock = variant.ProductSerials.Count(s => s.Status == ProductSerialStatus.InStock);
            int stockDifference = request.StockQuantity - currentStock;
            if (stockDifference > 0)
            {
                for (int i = 0; i < stockDifference; i++)
                {
                    string randomString = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                    string generatedSerial = $"SN-{variant.DetailId}-{i}-{randomString}";
                    variant.ProductSerials.Add(new ProductSerial
                    {
                        DetailId = variant.DetailId,
                        SerialNumber = generatedSerial,
                        Status = ProductSerialStatus.InStock,
                        ImportDate = DateTime.UtcNow
                    });
                }
            }
            else if (stockDifference < 0)
            {
                int numbersToRemove = Math.Abs(stockDifference);
                var SerialsToRemove = variant.ProductSerials.Where(s => s.Status == ProductSerialStatus.InStock).OrderByDescending(s => s.ImportDate).Take(numbersToRemove).ToList();
                foreach (var serial in SerialsToRemove)
                {
                    variant.ProductSerials.Remove(serial);
                }
            }
            variant.StockQuantity = variant.ProductSerials.Count(s => s.Status == ProductSerialStatus.InStock);
            await _productDetailRepository.UpdateAsync(variant);
            var res = variant.Adapt<ProductDetailAdminRespones>();
            res.TotalSerialNumbers = variant.ProductSerials?.Count ?? 0;
            return res;
        }
        public async Task<bool> DeleteVariantAsync(int productDetailId)
        {
            var variant = await _productDetailRepository.getProductDetailByIdAsync(productDetailId);
            if (variant == null) return false;
            bool hasSoldSerials = variant.ProductSerials.Any(s => s.Status == ProductSerialStatus.Sold || s.Status == ProductSerialStatus.Reserved);
            if (hasSoldSerials)
                throw new Exception("Không thể xóa variant vì đã có đơn hàng liên quan. Vui lòng kiểm tra lại.");
            await _productDetailRepository.DeleteAsync(productDetailId);
            return true;
        }

        public async Task<string> ImportProductDetailsFromExcelAsync(int productId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không được để trống.");

            if (!file.FileName.EndsWith(".xlsx"))
                throw new ArgumentException("Chỉ hỗ trợ file định dạng .xlsx");

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                throw new Exception($"Sản phẩm với ID {productId} không tồn tại.");

            var importedCount = 0;
            var errorMessages = new List<string>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet?.Dimension == null)
                        throw new ArgumentException("File Excel không có dữ liệu.");

                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            bool isEmptyRow = Enumerable.Range(2, 7)
                                .All(col => string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text));
                            if (isEmptyRow) continue;

                            if (!decimal.TryParse(worksheet.Cells[row, 7].Text, out var price) || price <= 0)
                            {
                                errorMessages.Add($"Dòng {row}: Giá bán không hợp lệ.");
                                continue;
                            }

                            if (!int.TryParse(worksheet.Cells[row, 8].Text, out var stockQuantity) || stockQuantity < 0)
                            {
                                errorMessages.Add($"Dòng {row}: Số lượng tồn kho không hợp lệ.");
                                continue;
                            }

                            int? maxTension = null;
                            var maxTensionText = worksheet.Cells[row, 6].Text.Trim();
                            if (!string.IsNullOrWhiteSpace(maxTensionText))
                            {
                                if (!int.TryParse(maxTensionText, out var parsedMaxTension))
                                {
                                    errorMessages.Add($"Dòng {row}: MaxTension không hợp lệ.");
                                    continue;
                                }

                                maxTension = parsedMaxTension;
                            }

                            var request = new CreateProductDetailRequest
                            {
                                WeightClass = worksheet.Cells[row, 2].Text.Trim(),
                                GripSize = worksheet.Cells[row, 3].Text.Trim(),
                                BalancePoint = worksheet.Cells[row, 4].Text.Trim(),
                                Stiffness = worksheet.Cells[row, 5].Text.Trim(),
                                MaxTension = maxTension,
                                Price = price,
                                StockQuantity = stockQuantity
                            };

                            await AddVariantAsync(productId, request);
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            errorMessages.Add($"Dòng {row}: {ex.Message}");
                        }
                    }
                }
            }

            string resultMessage = $"Đã import thành công {importedCount} chi tiết sản phẩm.";
            if (errorMessages.Any())
            {
                resultMessage += $"\nTuy nhiên, có {errorMessages.Count} dòng bị bỏ qua do lỗi:\n";
                resultMessage += string.Join("\n", errorMessages);
            }

            return resultMessage;
        }

        public async Task<byte[]> ExportProductDetailsToExcelAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                throw new Exception($"Sản phẩm với ID {productId} không tồn tại.");

            var (productDetails, _) = await _productRepository.GetProductDetailsByIdAsync(productId, 1, int.MaxValue);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("ProductDetails_Export");

                string[] headers = {
                    "Detail ID", "WeightClass", "GripSize", "BalancePoint", "Stiffness",
                    "MaxTension", "Price", "StockQuantity", "Total Serial"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                int row = 2;
                foreach (var detail in productDetails)
                {
                    worksheet.Cells[row, 1].Value = detail.DetailId;
                    worksheet.Cells[row, 2].Value = detail.WeightClass;
                    worksheet.Cells[row, 3].Value = detail.GripSize;
                    worksheet.Cells[row, 4].Value = detail.BalancePoint;
                    worksheet.Cells[row, 5].Value = detail.Stiffness;
                    worksheet.Cells[row, 6].Value = detail.MaxTension;
                    worksheet.Cells[row, 7].Value = detail.Price;
                    worksheet.Cells[row, 8].Value = detail.StockQuantity;
                    worksheet.Cells[row, 9].Value = detail.ProductSerials?.Count ?? 0;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }


        public async Task<VariantSerialsResponse> GetSerialNumbersByVariantIdAsync(int productDetailId, int page, int pageSize)
        {
            var variant = await _productDetailRepository.getProductDetailWithSerialNumberAsync(productDetailId);
            if (variant == null) throw new Exception($"Variant với ID {productDetailId} không tồn tại.");
            var specList = new List<string>();
            if (!string.IsNullOrWhiteSpace(variant.WeightClass)) specList.Add(variant.WeightClass);
            if (!string.IsNullOrWhiteSpace(variant.GripSize)) specList.Add(variant.GripSize);
            if (!string.IsNullOrWhiteSpace(variant.BalancePoint)) specList.Add(variant.BalancePoint);

            string variantInfo = string.Join(" - ", specList);
            var serials = variant.ProductSerials
                .OrderByDescending(s => s.ImportDate) // Sắp xếp theo ngày nhập (mới nhất lên đầu)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SerialNumberDto
                {
                    SerialId = s.SerialId,
                    SerialNumber = s.SerialNumber,
                    Status = s.Status,
                    ImportDate = s.ImportDate ?? DateTime.UtcNow
                }).ToList();
            return new VariantSerialsResponse
            {
                DetailId = variant.DetailId,
                VariantInfo = variantInfo,
                TotalCount = variant.ProductSerials.Count,
                InStockCount = variant.ProductSerials.Count(s => s.Status == ProductSerialStatus.InStock),
                SoldCount = variant.ProductSerials.Count(s => s.Status == ProductSerialStatus.Sold),
                DefectiveCount = variant.ProductSerials.Count(s => s.Status == ProductSerialStatus.Defective),
                ReservedCount = variant.ProductSerials.Count(s => s.Status == ProductSerialStatus.Reserved),
                Serials = serials
            };
        }
        public async Task<SerialNumberDto> AddSingleSerialNumberAsync(int productDetailId, CreateProductSerialRequest request)
        {
            var checkExistingSerial = await _productSerialRepository.IsSerialNumberExistsAsync(request.SerialNumber);
            if (checkExistingSerial)
                throw new Exception($"Số Serial '{request.SerialNumber}' đã tồn tại trong hệ thống. Vui lòng kiểm tra lại.");
            var variant = await _productDetailRepository.getProductDetailByIdAsync(productDetailId);
            if (variant == null) throw new Exception($"Variant với ID {productDetailId} không tồn tại.");
            var result = new ProductSerial
            {
                DetailId = productDetailId,
                SerialNumber = request.SerialNumber,
                Status = ProductSerialStatus.Normalized(request.Status),
                ImportDate = request.ImportDate ?? DateTime.UtcNow
            };
            variant.ProductSerials.Add(result);
            variant.StockQuantity = variant.ProductSerials.Count(s => s.Status == ProductSerialStatus.InStock);
            if (request.Status == ProductSerialStatus.Sold)
            {
                var product = await _productRepository.GetByIdAsync(variant.ProductId);
                if (product == null) throw new Exception("Sản phẩm liên quan đến variant không tồn tại.");
                product.SoldQuantity = (product.SoldQuantity ?? 0) + 1;
                await _productRepository.UpdateAsync(product);
            }
            await _productDetailRepository.UpdateAsync(variant);
            return new SerialNumberDto
            {
                SerialId = result.SerialId,
                SerialNumber = result.SerialNumber,
                Status = result.Status,
                ImportDate = result.ImportDate ?? DateTime.UtcNow
            };
        }

        // Quản lý Image
        public async Task<List<ProductImageResponse>> GetProductImagesAsync(int productId)
        {
            var images = await _productImageRepository.GetByProductIdAsync(productId);
            return images.Select(i => new ProductImageResponse
            {
                ImageId = i.ImageId,
                ImageUrl = i.ImageUrl,
                DisplayOrder = i.DisplayOrder,
                IsMain = i.IsMain ?? false
            }).ToList();
        }

        public async Task<ProductImageResponse> AddProductImageAsync(int productId, IFormFile file, bool isMain)
        {
            if (file == null || file.Length == 0)
                throw new Exception("Vui lòng chọn file ảnh.");

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) throw new Exception("Sản phẩm không tồn tại.");

            // Lưu file vào wwwroot/uploads/products/
            var uploadDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "products");
            Directory.CreateDirectory(uploadDir);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);
            await using (var stream = File.Create(filePath))
                await file.CopyToAsync(stream);
            var imageUrl = $"/uploads/products/{fileName}";

            // Lấy danh sách ảnh hiện tại xếp theo thứ tự tăng dần
            var images = await _productImageRepository.GetByProductIdAsync(productId);

            var newImg = new ProductImage
            {
                ProductId = productId,
                ImageUrl = imageUrl
            };

            // Trường hợp 1: Sản phẩm chưa có bất kỳ ảnh nào -> Bắt buộc ảnh này phải là Main (vị trí 1)
            if (!images.Any())
            {
                newImg.IsMain = true;
                newImg.DisplayOrder = 1;

                await _productImageRepository.AddAsync(newImg);

                product.MainImageUrl = imageUrl;
                await _productRepository.UpdateAsync(product);

                return new ProductImageResponse { ImageId = newImg.ImageId, ImageUrl = imageUrl, DisplayOrder = 1, IsMain = true };
            }

            // Trường hợp 2: Muốn đặt ảnh mới này làm ảnh chính luôn
            if (isMain)
            {
                // Đẩy TẤT CẢ các ảnh cũ tăng lên 1 bậc để nhường chỗ trống cho vị trí số 1
                foreach (var img in images)
                {
                    img.DisplayOrder += 1;
                    if (img.IsMain == true)
                    {
                        img.IsMain = false; // Hạ cờ ảnh chính cũ xuống
                    }
                    await _productImageRepository.UpdateAsync(img);
                }

                newImg.IsMain = true;
                newImg.DisplayOrder = 1;

                await _productImageRepository.AddAsync(newImg);

                // Đồng bộ cập nhật link ảnh đại diện cho bảng Product
                product.MainImageUrl = imageUrl;
                await _productRepository.UpdateAsync(product);
            }
            // Trường hợp 3: Thêm ảnh bình thường, không làm ảnh chính
            else
            {
                // Lấy DisplayOrder lớn nhất hiện tại để cộng thêm 1 (đẩy xuống cuối cùng)
                int maxOrder = images.Max(i => (int)i.DisplayOrder);
                newImg.IsMain = false;
                newImg.DisplayOrder = maxOrder + 1;

                await _productImageRepository.AddAsync(newImg);
            }

            return new ProductImageResponse
            {
                ImageId = newImg.ImageId,
                ImageUrl = newImg.ImageUrl,
                DisplayOrder = newImg.DisplayOrder,
                IsMain = newImg.IsMain ?? false
            };
        }

        public async Task<bool> SetMainImageAsync(int productId, int imageId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) throw new Exception("Sản phẩm không tồn tại.");

            var images = await _productImageRepository.GetByProductIdAsync(productId);
            var targetImg = images.FirstOrDefault(i => i.ImageId == imageId);
            if (targetImg == null) return false;

            int oldOrder = (int)targetImg.DisplayOrder; // Ví dụ: vị trí số 4

            // Nếu ảnh này đã ở vị trí số 1 và là Main rồi thì không cần xử lý nữa
            if (oldOrder == 1 && targetImg.IsMain == true) return true;

            foreach (var img in images)
            {
                // Những ảnh có vị trí đứng trước ảnh được chọn (từ 1 đến trước vị trí cũ)
                if (img.DisplayOrder < oldOrder)
                {
                    img.DisplayOrder += 1; // Đẩy lùi xuống 1 bước
                    img.IsMain = false;    // Hủy cờ Main nếu có
                }
                else if (img.ImageId == targetImg.ImageId)
                {
                    img.DisplayOrder = 1;  // Ảnh được chọn nhảy lên đầu
                    img.IsMain = true;
                }

                await _productImageRepository.UpdateAsync(img);
            }

            // Đồng bộ cập nhật lại bảng Product
            product.MainImageUrl = targetImg.ImageUrl;
            await _productRepository.UpdateAsync(product);

            return true;
        }

        public async Task<bool> UpdateImagesOrderAsync(int productId, List<UpdateImageOrderRequest> requests)
        {
            // 1. Kiểm tra đầu vào cơ bản
            if (requests == null || !requests.Any()) return false;

            // 2. BƯỚC BẢO MẬT: Kiểm tra xem Frontend có gửi trùng số thứ tự không?
            var hasDuplicateOrders = requests.GroupBy(x => x.DisplayOrder).Any(g => g.Count() > 1);
            if (hasDuplicateOrders)
            {
                throw new Exception("Dữ liệu vị trí từ giao diện bị trùng lặp, vui lòng tải lại trang.");
            }

            // 3. Lấy toàn bộ ảnh của sản phẩm này từ Database
            var currentImages = await _productImageRepository.GetByProductIdAsync(productId);
            if (!currentImages.Any()) return false;

            string? newMainImageUrl = null;

            // 4. Quét qua ảnh trong DB và gán giá trị mới từ Request
            foreach (var img in currentImages)
            {
                // Tìm xem ảnh này được Frontend chỉ định đứng thứ mấy
                var newOrderData = requests.FirstOrDefault(r => r.ImageId == img.ImageId);

                if (newOrderData != null)
                {
                    img.DisplayOrder = newOrderData.DisplayOrder;

                    // QUY TẮC THÉP: Chỉ ảnh ở vị trí số 1 mới được làm ảnh chính
                    if (newOrderData.DisplayOrder == 1)
                    {
                        img.IsMain = true;
                        newMainImageUrl = img.ImageUrl;
                    }
                    else
                    {
                        // Tất cả ảnh không nằm ở vị trí số 1 đều bị tước quyền làm ảnh chính
                        img.IsMain = false;
                    }
                }
            }
            await _productImageRepository.UpdateRangeAsync(currentImages);
            // 5. Đồng bộ cập nhật lại MainImageUrl cho bảng Product
            if (!string.IsNullOrEmpty(newMainImageUrl))
            {
                var product = await _productRepository.GetByIdAsync(productId);
                if (product != null && product.MainImageUrl != newMainImageUrl)
                {
                    product.MainImageUrl = newMainImageUrl;
                    await _productRepository.UpdateAsync(product);
                }
            }

            return true;
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            var imgToDelete = await _productImageRepository.GetByIdAsync(imageId);
            if (imgToDelete == null) return false;

            if (imgToDelete.IsMain == true)
            {
                throw new Exception("Không thể xóa ảnh đang được đặt làm ảnh đại diện chính. Vui lòng chọn ảnh khác làm đại diện trước.");
            }

            int deletedOrder = (int)imgToDelete.DisplayOrder;
            int productId = (int)imgToDelete.ProductId;

            // Xóa ảnh
            await _productImageRepository.DeleteAsync(imageId);

            // Lấy các ảnh còn lại và dồn thứ tự lên (Shift Up)
            var remainingImages = await _productImageRepository.GetByProductIdAsync(productId);
            foreach (var img in remainingImages.Where(i => i.DisplayOrder > deletedOrder))
            {
                img.DisplayOrder -= 1;
            }
            await _productImageRepository.UpdateRangeAsync(remainingImages.Where(i => i.DisplayOrder >= deletedOrder));
            return true;
        }
    }
}
