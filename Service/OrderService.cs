using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Repositories;

namespace MyOwnLearning.Service
{
    public interface IOrderService
    {
        Task<(List<OrderSummaryResponse> Orders, int TotalCount)> GetAllOrdersAsync(int page, int pageSize);
        Task<OrderResponse> GetOrderDetailForAdminAsync(int orderId);
        Task<List<OrderResponse>> GetMyOrdersAsync(int userId);
        Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request);
        Task<OrderResponse> UpdateOrderStatusAsync(int orderId, int newStatusId);
        Task<OrderResponse> CancelMyOrderAsync(int orderId, int userId);
        Task<OrderResponse> CancelOrderByAdminAsync(int orderId, int adminId, string reason);
        Task<(List<OrderSummaryResponse> Orders, int TotalCount)> GetOrdersByStatusIdAsync(int statusId, int page, int pageSize);
        Task<(List<OrderSummaryResponse> Orders, int TotalCount)> SearchOrderAdminAsync(decimal? minPrice, decimal? maxPrice, DateTime? orderDate, int? statusId, int page, int pageSize);
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

        private async Task RevertOrderResourcesAsync(Order order)
        {
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

            var voucherIds = order.OrderVouchers?.Select(ov => ov.VoucherId).ToList();
            if (voucherIds == null || !voucherIds.Any())
                return;

            foreach (var voucherId in voucherIds)
            {
                var voucher = await _context.Vouchers.FindAsync(voucherId);
                if (voucher != null && voucher.UsedCount > 0)
                    voucher.UsedCount--;

                var userVoucher = await _context.UserVouchers
                    .FirstOrDefaultAsync(uv => uv.UserId == order.UserId && uv.VoucherId == voucherId);

                if (userVoucher != null && userVoucher.CurrentUsageCount > 0)
                {
                    userVoucher.CurrentUsageCount--;

                    if (userVoucher.CurrentUsageCount == 0)
                        userVoucher.UsedDate = null;
                }
            }
        }

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
                CancelReason = o.CancelReason,
                CancelledAt = o.CancelledAt,
                CancelledByUserId = o.CancelledByUserId,
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

        public async Task<(List<OrderSummaryResponse> Orders, int TotalCount)> GetAllOrdersAsync(int page, int pageSize)
        {
            return await _orderRepository.GetAllOrderSummariesAsync(page, pageSize);
        }

        public async Task<OrderResponse> GetOrderDetailForAdminAsync(int orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => new OrderResponse
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    SubTotal = o.SubTotal,
                    TotalDiscount = o.TotalDiscount,
                    FinalAmount = o.FinalAmount,
                    ShippingFee = o.ShippingFee,
                    Status = o.OrderStatus != null ? o.OrderStatus.StatusName : "Chưa xác định",
                    ReceiverName = o.ReceiverName ?? string.Empty,
                    PhoneNumber = o.PhoneNumber,
                    ShippingAddress = o.ShippingAddress,
                    Note = o.Note,
                    CancelReason = o.CancelReason,
                    CancelledAt = o.CancelledAt,
                    CancelledByUserId = o.CancelledByUserId,
                    PaymentMethod = o.Payment != null ? o.Payment.PaymentMethod : "Chưa xác định",
                    AppliedVouchers = o.OrderVouchers
                        .Select(ov => new AppliedVoucherResponse
                        {
                            VoucherCode = ov.Voucher != null ? ov.Voucher.VoucherCode : string.Empty,
                            AppliedDiscount = ov.AppliedDiscount
                        })
                        .ToList(),
                    OrderDetails = o.OrderDetails
                        .OrderBy(od => od.OrderDetailId)
                        .Select(od => new OrderDetailResponse
                        {
                            OrderDetailId = od.OrderDetailId,
                            DetailId = od.DetailId,
                            Quantity = od.Quantity,
                            UnitPrice = od.UnitPrice,
                            IsStringingService = od.IsStringingService,
                            StringBrand = od.StringBrand,
                            TensionKg = od.TensionKg,
                            ProductName = od.Detail != null && od.Detail.Product != null
                                ? od.Detail.Product.ProductName
                                : string.Empty,
                            SerialNumbers = od.ProductSerials
                                .Select(ps => ps.SerialNumber)
                                .ToList()
                        })
                        .ToList()
                })
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

            return order;
        }

        public async Task<(List<OrderSummaryResponse> Orders, int TotalCount)> GetOrdersByStatusIdAsync(int statusId, int page, int pageSize)
        {
            return await _orderRepository.GetOrderSummariesByStatusIdAsync(statusId, page, pageSize);
        }

        public async Task<(List<OrderSummaryResponse> Orders, int TotalCount)> SearchOrderAdminAsync(
            decimal? minPrice, decimal? maxPrice, DateTime? orderDate, int? statusId, int page, int pageSize)
        {
            return await _orderRepository.SearchOrderSummaryAdminAsync(minPrice, maxPrice, orderDate, statusId, page, pageSize);
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
                        tempOrderItems,
                        request.PaymentMethod
                    );

                    if (!voucherResult.IsValid)
                        throw new InvalidOperationException(voucherResult.ErrorMessage);
                }

                // --- BƯỚC 2: Gọi Repository tạo đơn hàng, truyền vào voucher details ---
                // Repository sẽ ghi Order + OrderVoucher + lượt dùng voucher trong cùng một transaction.
                var order = await _orderRepository.CreateOrderAsync(
                    userId,
                    request,
                    voucherResult.AppliedVoucherDetails
                );

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

            if (newStatusId == (int)OrderStatusEnum.DaHuy)
                throw new InvalidOperationException("Vui lòng dùng API hủy đơn để nhập và lưu lý do hủy.");

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

                await RevertOrderResourcesAsync(order);

                order.OrderStatusId = (int)OrderStatusEnum.DaHuy;
                order.CancelReason = "Khách hàng yêu cầu hủy đơn.";
                order.CancelledAt = DateTime.UtcNow;
                order.CancelledByUserId = userId;
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

        public async Task<OrderResponse> CancelOrderByAdminAsync(int orderId, int adminId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Vui lòng nhập lý do hủy đơn.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);

                if (order.OrderStatusId == (int)OrderStatusEnum.DaHuy)
                    throw new InvalidOperationException("Đơn hàng đã được hủy trước đó.");

                var currentStatus = (OrderStatusEnum)order.OrderStatusId;
                if (!IsValidStatusTransition(currentStatus, OrderStatusEnum.DaHuy))
                    throw new InvalidOperationException($"Không thể hủy đơn hàng khi đang ở trạng thái {currentStatus}.");

                await RevertOrderResourcesAsync(order);

                order.OrderStatusId = (int)OrderStatusEnum.DaHuy;
                order.CancelReason = reason.Trim();
                order.CancelledAt = DateTime.UtcNow;
                order.CancelledByUserId = adminId;

                await _orderRepository.UpdateAsync(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _context.Entry(order).Reference(o => o.OrderStatus).LoadAsync();
                return MapToResponse(order);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Đã xảy ra lỗi khi Admin hủy đơn hàng: " + ex.Message);
            }
        }
    }
}
