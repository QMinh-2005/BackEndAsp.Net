using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Repositories;

namespace MyOwnLearning.Service
{
    public interface IOrderService
    {
        Task<(List<OrderResponse> Orders, int TotalCount)> GetAllOrdersAsync(int page, int pageSize);
        Task<List<OrderResponse>> GetMyOrdersAsync(int userId);
        Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request);
        Task<OrderResponse> UpdateOrderStatusAsync(int orderId, int newStatusId);
        Task<OrderResponse> CancelMyOrderAsync(int orderId, int userId);
        Task<(List<OrderResponse> Orders, int TotalCount)> GetOrdersByStatusIdAsync(int statusId, int page, int pageSize);
        Task<(List<OrderResponse> Orders, int TotalCount)> SearchOrderAdminAsync(decimal? minPrice, decimal? maxPrice, DateTime? orderDate, int? statusId, int page, int pageSize);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IVoucherService _voucherService;
        private readonly IProductDetailRepository _productDetailRepository;
        private readonly WebBadmintonContext _context;

        public OrderService(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IVoucherService voucherService,
            IProductDetailRepository productDetailRepository,
            WebBadmintonContext context)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _voucherService = voucherService;
            _context = context;
            _productDetailRepository = productDetailRepository;
        }

        // =====================================================
        // STATE MACHINE — Không đổi
        // =====================================================

        private static readonly Dictionary<OrderStatusEnum, List<OrderStatusEnum>> _validTransitions = new()
        {
            { OrderStatusEnum.ChoXacNhan,   new List<OrderStatusEnum> { OrderStatusEnum.DaXacNhan, OrderStatusEnum.DaHuy } },
            { OrderStatusEnum.DaXacNhan,    new List<OrderStatusEnum> { OrderStatusEnum.DangXuLy, OrderStatusEnum.DangDanLuoi, OrderStatusEnum.DaHuy } },
            { OrderStatusEnum.DangXuLy,     new List<OrderStatusEnum> { OrderStatusEnum.DangGiaoHang, OrderStatusEnum.DaHuy } },
            { OrderStatusEnum.DangDanLuoi,  new List<OrderStatusEnum> { OrderStatusEnum.DangXuLy, OrderStatusEnum.DangGiaoHang } },
            { OrderStatusEnum.DangGiaoHang, new List<OrderStatusEnum> { OrderStatusEnum.DaGiaoHang, OrderStatusEnum.DaHuy } },
            { OrderStatusEnum.DaGiaoHang,   new List<OrderStatusEnum> { OrderStatusEnum.HoanTat } },
            { OrderStatusEnum.HoanTat,      new List<OrderStatusEnum>() },
            { OrderStatusEnum.DaHuy,        new List<OrderStatusEnum>() }
        };

        private bool IsValidStatusTransition(OrderStatusEnum currentStatus, OrderStatusEnum newStatus)
            => _validTransitions.ContainsKey(currentStatus) && _validTransitions[currentStatus].Contains(newStatus);

        // =====================================================
        // HELPER — Map Order Entity → OrderResponse DTO
        // =====================================================

        private static OrderResponse MapToResponse(Order o)
        {
            return new OrderResponse
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                SubTotal = o.SubTotal,
                TotalDiscount = o.TotalDiscount,
                FinalAmount = o.FinalAmount,
                ShippingFee = o.ShippingFee,
                Status = o.OrderStatus?.StatusName ?? "Chưa xác định",
                ReceiverName = o.ReceiverName,
                PhoneNumber = o.PhoneNumber,
                ShippingAddress = o.ShippingAddress,
                Note = o.Note,
                PaymentMethod = o.Payment?.PaymentMethod ?? "Chưa xác định",
                // ✅ Map danh sách voucher đã áp dụng
                AppliedVouchers = o.OrderVouchers?.Select(ov => new AppliedVoucherResponse
                {
                    VoucherCode = ov.Voucher?.VoucherCode ?? string.Empty,
                    AppliedDiscount = ov.AppliedDiscount
                }).ToList() ?? new List<AppliedVoucherResponse>(),
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailResponse
                {
                    OrderDetailId = od.OrderDetailId,
                    DetailId = od.DetailId,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    IsStringingService = od.IsStringingService,
                    StringBrand = od.StringBrand,
                    TensionKg = od.TensionKg,
                    ProductName = od.Detail?.Product?.ProductName,
                    SerialNumbers = od.ProductSerials?.Select(ps => ps.SerialNumber).ToList()
                                        ?? new List<string>()
                }).ToList()
            };
        }

        // =====================================================
        // GET
        // =====================================================

        public async Task<List<OrderResponse>> GetMyOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
            return orders.Select(MapToResponse).OrderByDescending(o => o.OrderDate).ToList();
        }

        public async Task<(List<OrderResponse> Orders, int TotalCount)> GetAllOrdersAsync(int page, int pageSize)
        {
            var (orders, totalCount) = await _orderRepository.GetAllOrdersWithDetailsAsync(page, pageSize);
            return (orders.Select(MapToResponse).ToList(), totalCount);
        }

        public async Task<(List<OrderResponse> Orders, int TotalCount)> GetOrdersByStatusIdAsync(int statusId, int page, int pageSize)
        {
            var (orders, totalCount) = await _orderRepository.GetOrdersByStatusIdAsync(statusId, page, pageSize);
            return (orders.Select(MapToResponse).ToList(), totalCount);
        }

        public async Task<(List<OrderResponse> Orders, int TotalCount)> SearchOrderAdminAsync(
            decimal? minPrice, decimal? maxPrice, DateTime? orderDate, int? statusId, int page, int pageSize)
        {
            var (orders, totalCount) = await _orderRepository.SearchOrderAdminAsync(minPrice, maxPrice, orderDate, statusId, page, pageSize);
            return (orders.Select(MapToResponse).ToList(), totalCount);
        }

        // =====================================================
        // CREATE ORDER — Tích hợp Voucher đầy đủ
        // =====================================================

        public async Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request)
        {
            try
            {
                // --- BƯỚC 1: Validate Voucher & tính discount TRƯỚC khi tạo đơn ---
                // ✅ VoucherService chỉ validate, không ghi DB ở bước này
                var voucherResult = new VoucherValidationResult { IsValid = true };

                if (request.VoucherIds != null && request.VoucherIds.Any())
                {
                    var tempOrderItems = new List<OrderDetail>();

                    foreach (var od in request.OrderDetails)
                    {
                        // Cần có _context hoặc Repository để lấy thông tin ProductDetail dựa trên DetailId
                        var productDetail = await _productDetailRepository.getProductDetailByIdAsync(od.DetailId);

                        if (productDetail == null) throw new Exception($"Không tìm thấy sản phẩm có DetailId = {od.DetailId}");

                        tempOrderItems.Add(new OrderDetail
                        {
                            // KHÔNG gán OrderDetailId nữa vì nó chưa tồn tại
                            DetailId = od.DetailId,
                            Quantity = od.Quantity,
                            UnitPrice = productDetail.Price // LẤY GIÁ THẬT TỪ DATABASE VÀO ĐÂY
                        });
                    }
                    voucherResult = await _voucherService.ValidateAndCalculateDiscountAsync(
                        userId,
                        request.VoucherIds,
                        tempOrderItems
                    );

                    if (!voucherResult.IsValid)
                        throw new InvalidOperationException(voucherResult.ErrorMessage);
                }

                // --- BƯỚC 2: Gọi Repository tạo đơn hàng, truyền vào voucher details ---
                // ✅ Repository sẽ ghi Order + OrderVoucher trong 1 transaction duy nhất
                var order = await _orderRepository.CreateOrderAsync(
                    userId,
                    request,
                    voucherResult.AppliedVoucherDetails
                );

                // --- BƯỚC 3: Cập nhật lượt dùng Voucher SAU KHI đơn hàng đã lưu thành công ---
                // ✅ Tách riêng để nếu bước này lỗi không rollback cả đơn hàng
                if (request.VoucherIds != null && request.VoucherIds.Any())
                {
                    await _voucherService.UpdateVoucherUsageAsync(userId, request.VoucherIds);
                }

                return MapToResponse(order);
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi tạo đơn hàng: " + ex.Message);
            }
        }

        // =====================================================
        // UPDATE STATUS
        // =====================================================

        public async Task<OrderResponse> UpdateOrderStatusAsync(int orderId, int newStatusId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (!Enum.IsDefined(typeof(OrderStatusEnum), newStatusId))
                throw new ArgumentException("Trạng thái mới không hợp lệ.");

            var currentStatus = (OrderStatusEnum)order.OrderStatusId;
            var nextStatus = (OrderStatusEnum)newStatusId;

            if (currentStatus == nextStatus)
                throw new ArgumentException("Đơn hàng đang ở trạng thái này rồi.");

            if (!IsValidStatusTransition(currentStatus, nextStatus))
                throw new InvalidOperationException($"Không thể chuyển trạng thái từ {currentStatus} sang {nextStatus}.");

            var updatedOrder = await _orderRepository.UpdateStatusOrderAsync(orderId, newStatusId);
            return MapToResponse(updatedOrder);
        }

        // =====================================================
        // CANCEL MY ORDER — Thêm hoàn lại lượt dùng Voucher
        // =====================================================

        public async Task<OrderResponse> CancelMyOrderAsync(int orderId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetOrderByIdAndUserIdAsync(orderId, userId);

                if (order.OrderStatusId == (int)OrderStatusEnum.DaHuy)
                    throw new InvalidOperationException("Đơn hàng đã được hủy trước đó.");

                if (order.OrderStatusId != (int)OrderStatusEnum.ChoXacNhan &&
                    order.OrderStatusId != (int)OrderStatusEnum.DaXacNhan)
                    throw new InvalidOperationException("Chỉ có thể hủy đơn hàng khi đang ở trạng thái 'Chờ xác nhận' hoặc 'Đã xác nhận'.");

                // --- Revert Stock, SoldQuantity, ProductSerial ---
                foreach (var orderDetail in order.OrderDetails)
                {
                    if (orderDetail.Detail != null)
                    {
                        orderDetail.Detail.StockQuantity += orderDetail.Quantity;

                        if (orderDetail.Detail.Product != null)
                            orderDetail.Detail.Product.SoldQuantity -= orderDetail.Quantity;

                        _context.ProductDetails.Update(orderDetail.Detail);
                    }

                    var serialsToRevert = await _context.ProductSerials
                        .Where(ps => ps.OrderDetailId == orderDetail.OrderDetailId)
                        .ToListAsync();

                    foreach (var serial in serialsToRevert)
                    {
                        serial.Status = ProductSerialStatus.InStock;
                        serial.OrderDetailId = null;
                    }
                }

                // ✅ Hoàn lại lượt dùng Voucher khi hủy đơn
                var voucherIds = order.OrderVouchers?.Select(ov => ov.VoucherId).ToList();
                if (voucherIds != null && voucherIds.Any())
                {
                    foreach (var voucherId in voucherIds)
                    {
                        var voucher = await _context.Vouchers.FindAsync(voucherId);
                        if (voucher != null && voucher.UsedCount > 0)
                            voucher.UsedCount--;

                        var userVoucher = await _context.UserVouchers
                            .FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);

                        if (userVoucher != null && userVoucher.CurrentUsageCount > 0)
                        {
                            userVoucher.CurrentUsageCount--;
                            // Reset UsedDate nếu về 0 lượt
                            if (userVoucher.CurrentUsageCount == 0)
                                userVoucher.UsedDate = null;
                        }
                    }
                }

                order.OrderStatusId = (int)OrderStatusEnum.DaHuy;
                await _orderRepository.UpdateAsync(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _context.Entry(order).Reference(o => o.OrderStatus).LoadAsync();
                return MapToResponse(order);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Đã xảy ra lỗi khi hủy đơn hàng: " + ex.Message);
            }
        }
    }
}