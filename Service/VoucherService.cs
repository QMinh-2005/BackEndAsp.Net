using System.Threading.Tasks;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Repositories;

namespace MyOwnLearning.Service
{
    public class VoucherValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public decimal TotalDiscount { get; set; }
        public List<AppliedVoucherDetail> AppliedVoucherDetails { get; set; } = new();
    }
    public class AppliedVoucherDetail
    {
        public int VoucherId { get; set; }
        public decimal DiscountValue { get; set; }
    }
    public interface IVoucherService
    {
        Task<VoucherValidationResult> ValidateAndCalculateDiscountAsync(int userId, List<int> VoucherIds, List<OrderDetail> orderItems, string paymentMethod);
        Task UpdateVoucherUsageAsync(int userId, List<int> voucherIds);
        Task<List<VoucherDisplayResponse>> GetAvailableVouchersForUserAsync(int userId, ApplicableVoucherRequest request);
        Task<List<VoucherResponse>> GetAllVouchersForUserAsync();
        Task<bool> SaveVoucherAsync(int userId, int voucherId);
        Task<Voucher> CreateVoucherAsync(VoucherCreateRequest request);
        Task<(List<VoucherAdminResponse> Vouchers, int TotalCount)> GetVouchersForAdminAsync(
            string? keyword,
            bool? isActive,
            bool? isGlobal,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize);
        Task<bool> SetVoucherActiveAsync(int voucherId, bool isActive);

    }

    public class VoucherService : IVoucherService
    {
        private readonly IVoucherRepository _voucherRepository;
        private readonly IUserVoucherRepository _userVoucherRepository;
        private readonly IProductDetailRepository _productDetailRepository;
        private readonly IOrderRepository _orderRepository;

        public VoucherService(
            IVoucherRepository voucherRepository,
            IUserVoucherRepository userVoucherRepository,
            IProductDetailRepository productDetailRepository,
            IOrderRepository orderRepository)
        {
            _voucherRepository = voucherRepository;
            _userVoucherRepository = userVoucherRepository;
            _productDetailRepository = productDetailRepository;
            _orderRepository = orderRepository;
        }
        private bool IsValidPaymentMethodForVoucher(Voucher voucher, string paymentMethod)
        {
            if (voucher.VoucherPaymentMethods == null || !voucher.VoucherPaymentMethods.Any())
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                return true;
            }
            var allowPaymentMethod = voucher.VoucherPaymentMethods.Select(x => x.PaymentMethod.Trim()).ToList();
            return allowPaymentMethod.Contains(paymentMethod.Trim(), StringComparer.OrdinalIgnoreCase);
        }
        public async Task<VoucherValidationResult> ValidateAndCalculateDiscountAsync(int userId, List<int> VoucherIds, List<OrderDetail> orderItems, string paymentMethod)
        {
            var result = new VoucherValidationResult { IsValid = true };
            decimal totalOrderDiscount = 0;
            decimal totalOrderSubTotal = orderItems.Sum(item => item.Quantity * item.UnitPrice);
            foreach (var vId in VoucherIds)
            {
                var voucher = await _voucherRepository.GetVoucherByIdAsync(vId);
                if (voucher == null || voucher.IsActive == false || voucher.EndDate < DateTime.UtcNow)
                {
                    return Error("Voucher không tồn tại hoặc đã hết hạn.");
                }
                if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
                {
                    return Error($"Mã {voucher.VoucherCode} đã hết lượt sử dụng.");
                }
                if (!IsValidPaymentMethodForVoucher(voucher, paymentMethod))
                {
                    var allowedMethods = voucher.VoucherPaymentMethods.Select(pm => pm.PaymentMethod);
                    string methodsStr = string.Join(" hoặc ", allowedMethods);
                    return Error($"Mã {voucher.VoucherCode} chỉ áp dụng khi thanh toán qua: {methodsStr}.");
                }
                if (voucher.IsGlobal == true)
                {
                    // Mã Global: Đếm trực tiếp trong lịch sử đơn hàng thành công
                    int usedTimes = await _orderRepository.CountSuccessfulUsesAsync(userId, vId);
                    if (usedTimes >= voucher.MaxUsagePerUser)
                        return Error($"Bạn đã dùng hết lượt cho phép của mã toàn sàn {voucher.VoucherCode}.");
                }
                else
                {
                    // Mã Cá nhân: Bắt buộc phải có trong ví và kiểm tra CurrentUsageCount
                    var userVoucher = await _userVoucherRepository.GetUserVoucherAsync(userId, vId);
                    if (userVoucher == null)
                        return Error($"Bạn chưa sở hữu mã {voucher.VoucherCode} trong ví.");

                    if (userVoucher.CurrentUsageCount >= voucher.MaxUsagePerUser)
                        return Error($"Bạn đã dùng hết lượt cho phép của mã {voucher.VoucherCode}.");
                }
                List<OrderDetail> eligibleItems;
                if (voucher.IsGlobal == true)
                {
                    eligibleItems = orderItems;
                }
                else
                {
                    eligibleItems = new List<OrderDetail>();
                    foreach (var item in orderItems)
                    {
                        if (!item.DetailId.HasValue) continue;
                        var productDetail = await _productDetailRepository.getProductDetailByIdAsync(item.DetailId.Value);
                        if (productDetail == null) continue;
                        bool match = voucher.VoucherConditions.Any(c =>
                            (c.ProductId == null || c.ProductId == productDetail.ProductId) &&
                            (c.CategoryId == null || c.CategoryId == productDetail.Product.CategoryId) &&
                            (c.BrandId == null || c.BrandId == productDetail.Product.BrandId)
                        );
                        if (match) eligibleItems.Add(item);
                    }
                }
                if (!eligibleItems.Any())
                    return Error($"Mã {voucher.VoucherCode} không áp dụng cho các sản phẩm bạn chọn.");
                decimal eligibleSubTotal = eligibleItems.Sum(x => x.Quantity * x.UnitPrice);
                if (eligibleSubTotal < voucher.MinOrderValue)
                    return Error($"Mã {voucher.VoucherCode} yêu cầu đơn hàng từ {voucher.MinOrderValue:N0}đ.");
                decimal discount = 0;
                if (voucher.IsPercent == true)
                {
                    discount = eligibleSubTotal * (voucher.DiscountValue / 100m);
                    if (discount > voucher.MaxDiscountAmount) discount = voucher.MaxDiscountAmount.Value;
                }
                else
                {
                    discount = voucher.DiscountValue;
                }

                result.AppliedVoucherDetails.Add(new AppliedVoucherDetail { VoucherId = vId, DiscountValue = discount });
                totalOrderDiscount += discount;

            }
            result.TotalDiscount = Math.Min(totalOrderDiscount, totalOrderSubTotal);
            return result;
        }


        public async Task UpdateVoucherUsageAsync(int userId, List<int> voucherIds)
        {
            foreach (var vId in voucherIds)
            {
                var voucher = await _voucherRepository.GetByIdAsync(vId);
                var userVoucher = await _userVoucherRepository.GetUserVoucherAsync(userId, vId);

                if (voucher != null) voucher.UsedCount++;
                if (userVoucher != null)
                {
                    userVoucher.CurrentUsageCount++;
                    userVoucher.UsedDate = DateTime.Now;
                }
            }
            await _voucherRepository.SaveChangesAsync();
        }
        private VoucherValidationResult Error(string message) => new() { IsValid = false, ErrorMessage = message };
        public async Task<List<VoucherDisplayResponse>> GetAvailableVouchersForUserAsync(int userId, ApplicableVoucherRequest request)
        {
            request ??= new ApplicableVoucherRequest();

            // Lấy voucher global còn lượt của user và voucher cá nhân user đã lưu trong ví.
            var vouchers = await _voucherRepository.GetVouchersForDropdownAsync(userId);
            var result = new List<VoucherDisplayResponse>();
            var orderItems = request.OrderItems ?? new List<OrderItemRequest>();
            var hasOrderItems = orderItems.Any();

            var cartItems = new List<(int Quantity, decimal UnitPrice, ProductDetail ProductDetail)>();
            if (hasOrderItems)
            {
                foreach (var item in orderItems)
                {
                    var pd = await _productDetailRepository.getProductDetailByIdAsync(item.DetailId);
                    if (pd != null) cartItems.Add((item.Quantity, item.UnitPrice, pd));
                }
            }

            foreach (var v in vouchers)
            {
                var dto = new VoucherDisplayResponse
                {
                    VoucherId = v.VoucherId,
                    VoucherCode = v.VoucherCode,
                    Description = v.Description,
                    DiscountValue = v.DiscountValue,
                    IsPercent = v.IsPercent,
                    MaxDiscountAmount = v.MaxDiscountAmount,
                    MinOrderValue = v.MinOrderValue ?? 0,
                    EndDate = v.EndDate,
                    IsGlobal = v.IsGlobal ?? false,
                    AllowedPaymentMethods = v.VoucherPaymentMethods?.Select(pm => pm.PaymentMethod).ToList() ?? new List<string>(),
                    IsEligible = true,
                    DisabledReason = null
                };

                if (!string.IsNullOrEmpty(request.PaymentMethod))
                {
                    var allowedMethods = dto.AllowedPaymentMethods;
                    if (allowedMethods.Any() && !allowedMethods.Contains(request.PaymentMethod.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        dto.IsEligible = false;
                        dto.DisabledReason = $"Mã này chỉ áp dụng cho phương thức: {string.Join(", ", allowedMethods)}";
                        result.Add(dto);
                        continue;
                    }
                }

                // Nếu FE chỉ mở màn "Voucher của tôi" và chưa gửi sản phẩm trong đơn,
                // API chỉ đánh giá theo thời hạn, lượt dùng và phương thức thanh toán.
                if (!hasOrderItems)
                {
                    result.Add(dto);
                    continue;
                }

                decimal eligibleSubTotal = 0;
                if (v.IsGlobal == true || v.VoucherConditions == null || !v.VoucherConditions.Any())
                {
                    eligibleSubTotal = cartItems.Sum(x => x.Quantity * x.UnitPrice);
                }
                else
                {
                    var eligibleItems = cartItems.Where(x => v.VoucherConditions.Any(c =>
                        (c.ProductId == null || c.ProductId == x.ProductDetail.ProductId) &&
                        (c.CategoryId == null || c.CategoryId == x.ProductDetail.Product.CategoryId) &&
                        (c.BrandId == null || c.BrandId == x.ProductDetail.Product.BrandId)
                    )).ToList();

                    if (!eligibleItems.Any())
                    {
                        dto.IsEligible = false;
                        dto.DisabledReason = "Mã không áp dụng cho bất kỳ sản phẩm nào trong đơn hàng này.";
                        result.Add(dto);
                        continue;
                    }

                    eligibleSubTotal = eligibleItems.Sum(x => x.Quantity * x.UnitPrice);
                }

                if (v.MinOrderValue.HasValue && eligibleSubTotal < v.MinOrderValue.Value)
                {
                    dto.IsEligible = false;
                    decimal missingAmount = v.MinOrderValue.Value - eligibleSubTotal;
                    dto.DisabledReason = $"Đơn hàng chưa đạt giá trị tối thiểu. Cần mua thêm {missingAmount:N0}đ các sản phẩm phù hợp.";
                }

                result.Add(dto);
            }

            return result
                .OrderByDescending(x => x.IsEligible)
                .ThenBy(x => x.EndDate)
                .ToList();
        }
        public async Task<List<VoucherResponse>> GetAllVouchersForUserAsync()
        {
            var voucher = await _voucherRepository.GetAllAvailableVouchersAsync();
            return voucher.Select(v => new VoucherResponse
            {
                VoucherId = v.VoucherId,
                VoucherCode = v.VoucherCode,
                Description = v.Description,
                DiscountValue = v.DiscountValue,
                IsPercent = v.IsPercent,
                MaxDiscountAmount = v.MaxDiscountAmount,
                MinOrderValue = v.MinOrderValue ?? 0,
                StartDate = v.StartDate,
                EndDate = v.EndDate,
                MaxUsagePerUser = v.MaxUsagePerUser,
                IsGlobal = v.IsGlobal ?? false,
                AllowedPaymentMethods = v.VoucherPaymentMethods != null
                          ? v.VoucherPaymentMethods.Select(pm => pm.PaymentMethod).ToList()
                          : new List<string>()
            }).ToList();
        }
        public async Task<bool> SaveVoucherAsync(int userId, int voucherId)
        {
            var voucher = await _voucherRepository.GetVoucherByIdAsync(voucherId);
            if (voucher == null) throw new Exception("Voucher không tồn tại.");

            // Thường mã Global (toàn hệ thống) không cần lưu vào ví cá nhân
            if (voucher.IsGlobal == true) throw new Exception("Đây là mã dùng chung, không cần lưu.");

            var isSaved = await _userVoucherRepository.IsVoucherAlreadySavedAsync(userId, voucherId);
            if (isSaved) throw new Exception("Bạn đã lưu voucher này rồi.");

            var userVoucher = new UserVoucher
            {
                UserId = userId,
                VoucherId = voucherId,
                CurrentUsageCount = 0,
                UsedDate = null,
                SavedDate = DateTime.Now
            };

            await _userVoucherRepository.AddAsync(userVoucher);
            await _voucherRepository.SaveChangesAsync();
            return true;
        }
        public async Task<Voucher> CreateVoucherAsync(VoucherCreateRequest request)
        {
            var voucher = new Voucher
            {
                VoucherCode = request.VoucherCode,
                Description = request.Description,
                DiscountValue = request.DiscountValue,
                IsPercent = request.IsPercent,
                MaxDiscountAmount = request.MaxDiscountAmount,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MinOrderValue = request.MinOrderValue,
                IsGlobal = request.IsGlobal,
                MaxUsagePerUser = request.MaxUsagePerUser,
                UsageLimit = request.UsageLimit,
                UsedCount = 0,
                IsActive = true,
            };

            if (request.Conditions != null)
            {
                voucher.VoucherConditions = request.Conditions.Select(c => new VoucherCondition
                {
                    ProductId = c.ProductId,
                    CategoryId = c.CategoryId,
                    BrandId = c.BrandId
                }).ToList();
            }
            if (request.AllowedPaymentMethods != null)
            {
                voucher.VoucherPaymentMethods = request.AllowedPaymentMethods.Select(pm => new VoucherPaymentMethod
                {
                    PaymentMethod = pm
                }).ToList();
            }
            await _voucherRepository.AddAsync(voucher);
            await _voucherRepository.SaveChangesAsync();
            return voucher;
        }

        public async Task<(List<VoucherAdminResponse> Vouchers, int TotalCount)> GetVouchersForAdminAsync(
            string? keyword,
            bool? isActive,
            bool? isGlobal,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            {
                throw new Exception("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            }

            return await _voucherRepository.GetVouchersForAdminAsync(
                keyword,
                isActive,
                isGlobal,
                fromDate,
                toDate,
                page,
                pageSize);
        }

        public async Task<bool> SetVoucherActiveAsync(int voucherId, bool isActive)
        {
            return await _voucherRepository.SetVoucherActiveAsync
                (voucherId, isActive);
        }
    }
}
