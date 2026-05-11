using System.Text.RegularExpressions;
using Mapster;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Helpers;

namespace MyOwnLearning.Service
{
    public interface IProductService
    {
        Task<List<ProductHomeResponse>> GetProductsForHomePageAsync();
        Task<(List<Product> products, int TotalCount)> SearchAsync(string? categorySlug, string? brandSlug, string? key, decimal? minPrice, decimal? maxPrice, bool? Voucher, bool? isBestSeller, string? sortBy, int page, int pageSize);
        string GenerateSlug(string categorySlug, string title);
        Task<(List<ProductResponse> products, int TotalCount)> GetProductByCategorySlugAsync(string categorySlug, int page, int pageSize);
        Task<ProductDetailResponse?> GetProductDetailAsync(string slug);

        //Trang 1
        Task<(List<ProductAdminResponse> products, int TotalCount)> GetProductsForAdminAsync(string? keyword, int? categoryId, int? brandId, int page, int pageSize);
        Task<Product> CreateProductAsync(CreateProductRequest request);
        Task<Product> UpdateProductAsync(int idPro, UpdateProductRequest request);
        Task<bool> DeleteProductAsync(int productId);


        //Trang 2

        Task<(List<ProductDetailAdminRespones> productDetails, int TotalCount)> GetProductDetailsByIdAsync(int productId, int page, int pageSize);
        Task<ProductDetailAdminRespones> AddVariantAsync(int productId, CreateProductDetailRequest request);
        Task<ProductDetailAdminRespones> UpdateVariantAsync(int productDetailId, UpdateProductDetailRequest request);
        Task<bool> DeleteVariantAsync(int productDetailId);

        //Trang 3: Các phương thức liên quan đến quản lý SerialNumber sẽ được thêm sau khi hoàn thành phần quản lý Variant, vì SerialNumber phụ thuộc vào Variant (ProductDetail)
        Task<VariantSerialsResponse> GetSerialNumbersByVariantIdAsync(int productDetailId, int page, int pageSize);
        Task<SerialNumberDto> AddSingleSerialNumberAsync(int productDetailId, CreateProductSerialRequest request);
    }
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductDetailRepository _productDetailRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IProductSerialRepository _productSerialRepository;
        public ProductService(IProductRepository productRepository, IProductDetailRepository productDetailRepository, ICategoryRepository categoryRepository, IBrandRepository brandRepository, IProductSerialRepository productSerialRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _productSerialRepository = productSerialRepository;
            _productDetailRepository = productDetailRepository;
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

        //add 1 sản phẩm
        public async Task<(List<ProductAdminResponse> products, int TotalCount)> GetProductsForAdminAsync(string? keyword, int? categoryId, int? brand, int page, int pageSize)
        {
            var (products, totalCount) = await _productRepository.GetProductsForAdminAsync(keyword, categoryId, brand, page, pageSize);
            var response = products.Select(p => new ProductAdminResponse
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                MainImageUrl = p.MainImageUrl,
                BasePrice = p.BasePrice,
                DiscountPrice = p.DiscountPrice,
                DiscountPercent = p.DiscountPrice.HasValue && p.BasePrice > 0
                    ? (int)Math.Round((p.BasePrice - p.DiscountPrice.Value) / p.BasePrice * 100)
                    : 0,
                BrandName = p.Brand != null ? p.Brand.BrandName : "N/A",
                CategoryName = p.Category != null ? p.Category.CategoryName : "N/A",
                VariantsCount = p.ProductDetails?.Count ?? 0,
                TotalStock = p.ProductDetails?.Sum(d => d.StockQuantity ?? 0) ?? 0,
                SoldQuantity = p.SoldQuantity ?? 0,
            }).ToList();
            return (response, totalCount);
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
            var (existingVariant, _) = await _productRepository.GetProductDetailsByIdAsync(productId, 1, 100);
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
                SerialNumber = result.SerialNumber,
                Status = result.Status,
                ImportDate = result.ImportDate ?? DateTime.UtcNow
            };
        }
    }
}