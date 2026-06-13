using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Service
{
    public interface IOrderService
    {
        Task<(List<OrderSummaryResponse> Orders, int TotalCount)> GetAllOrdersAsync(int page, int pageSize);
        Task<OrderResponse> GetOrderDetailForAdminAsync(int orderId);
        Task<List<OrderResponse>> GetMyOrdersAsync(int userId);
        Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request);
        Task<OrderResponse> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request);
        Task<OrderResponse> CancelMyOrderAsync(int orderId, int userId);
        Task<OrderResponse> CancelOrderByAdminAsync(int orderId, int adminId, string reason);
        Task<(List<OrderSummaryResponse> Orders, int TotalCount)> GetOrdersByStatusIdAsync(int statusId, int page, int pageSize);
        Task<(List<OrderSummaryResponse> Orders, int TotalCount)> SearchOrderAdminAsync(decimal? minPrice, decimal? maxPrice, DateTime? orderDate, int? statusId, int page, int pageSize);
    }

    public class OrderService : IOrderService
    {
        private static readonly Dictionary<OrderStatusEnum, List<OrderStatusEnum>> _validTransitions = new()
        {
            { OrderStatusEnum.ChoXacNhan,   new List<OrderStatusEnum> { OrderStatusEnum.DaXacNhan, OrderStatusEnum.DaHuy } },
            { OrderStatusEnum.DaXacNhan,    new List<OrderStatusEnum> { OrderStatusEnum.DangXuLy, OrderStatusEnum.DangDanLuoi, OrderStatusEnum.DaHuy } },
            { OrderStatusEnum.DangXuLy,     new List<OrderStatusEnum> { OrderStatusEnum.DangGiaoHang, OrderStatusEnum.DaHuy } },
            { OrderStatusEnum.DangDanLuoi,  new List<OrderStatusEnum> { OrderStatusEnum.DangXuLy, OrderStatusEnum.DangGiaoHang } },
            { OrderStatusEnum.DangGiaoHang, new List<OrderStatusEnum> { OrderStatusEnum.DaGiaoHang} },
            { OrderStatusEnum.DaGiaoHang,   new List<OrderStatusEnum> { OrderStatusEnum.HoanTat } },
            { OrderStatusEnum.HoanTat,      new List<OrderStatusEnum>() },
            { OrderStatusEnum.DaHuy,        new List<OrderStatusEnum>() },
            { OrderStatusEnum.DangYeuCauTraHangHoanTien, new List<OrderStatusEnum>() },
            { OrderStatusEnum.DaChapThuanTraHangHoanTien, new List<OrderStatusEnum>() },
            { OrderStatusEnum.DaHoanHangHoanTien, new List<OrderStatusEnum>() }
        };

        private static readonly int[] CustomerCancelableStatusIds =
        {
            (int)OrderStatusEnum.ChoXacNhan,
            (int)OrderStatusEnum.DaXacNhan
        };

        private readonly IOrderRepository _orderRepository;
        private readonly IVoucherService _voucherService;
        private readonly IProductDetailRepository _productDetailRepository;

        public OrderService(
            IOrderRepository orderRepository,
            IVoucherService voucherService,
            IProductDetailRepository productDetailRepository)
        {
            _orderRepository = orderRepository;
            _voucherService = voucherService;
            _productDetailRepository = productDetailRepository;
        }

        private static bool IsValidStatusTransition(OrderStatusEnum currentStatus, OrderStatusEnum newStatus)
            => _validTransitions.ContainsKey(currentStatus) && _validTransitions[currentStatus].Contains(newStatus);

        private static int[] GetAdminCancelableStatusIds()
        {
            return _validTransitions
                .Where(x => x.Value.Contains(OrderStatusEnum.DaHuy))
                .Select(x => (int)x.Key)
                .ToArray();
        }

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
                StatusId = o.OrderStatusId,
                Status = o.OrderStatus?.StatusName ?? "Chưa xác định",
                ReceiverName = o.ReceiverName,
                PhoneNumber = o.PhoneNumber,
                ShippingAddress = o.ShippingAddress,
                Note = o.Note,
                CancelReason = o.CancelReason,
                CancelledAt = o.CancelledAt,
                CancelledByUserId = o.CancelledByUserId,
                PaymentMethod = o.Payment?.PaymentMethod ?? "Chưa xác định",
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
            var order = await _orderRepository.GetOrderDetailForAdminAsync(orderId);

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

        public async Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request)
        {
            try
            {
                var voucherResult = new VoucherValidationResult { IsValid = true };

                if (request.VoucherIds != null && request.VoucherIds.Any())
                {
                    var tempOrderItems = new List<OrderDetail>();

                    foreach (var od in request.OrderDetails)
                    {
                        var productDetail = await _productDetailRepository.getProductDetailByIdAsync(od.DetailId);

                        if (productDetail == null)
                            throw new Exception($"Không tìm thấy sản phẩm có DetailId = {od.DetailId}");

                        tempOrderItems.Add(new OrderDetail
                        {
                            DetailId = od.DetailId,
                            Quantity = od.Quantity,
                            UnitPrice = productDetail.Price
                        });
                    }

                    voucherResult = await _voucherService.ValidateAndCalculateDiscountAsync(
                        userId,
                        request.VoucherIds,
                        tempOrderItems,
                        request.PaymentMethod);

                    if (!voucherResult.IsValid)
                        throw new InvalidOperationException(voucherResult.ErrorMessage);
                }

                var order = await _orderRepository.CreateOrderAsync(
                    userId,
                    request,
                    voucherResult.AppliedVoucherDetails);

                return MapToResponse(order);
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi tạo đơn hàng: " + ex.Message);
            }
        }

        public async Task<OrderResponse> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request)
        {
            if (request == null)
                throw new ArgumentException("Dữ liệu cập nhật trạng thái không hợp lệ.");

            var newStatusId = request.NewOrderStatusId;

            if (!Enum.IsDefined(typeof(OrderStatusEnum), newStatusId))
                throw new ArgumentException("Trạng thái mới không hợp lệ.");

            if (newStatusId == (int)OrderStatusEnum.DaHuy)
                throw new InvalidOperationException("Vui lòng dùng API hủy đơn để nhập và lưu lý do hủy.");

            var currentStatusId = await _orderRepository.GetOrderStatusIdAsync(orderId);

            if (!currentStatusId.HasValue)
                throw new Exception("Không tìm thấy đơn hàng.");

            var currentStatus = (OrderStatusEnum)currentStatusId.Value;
            var nextStatus = (OrderStatusEnum)newStatusId;

            if (currentStatus == nextStatus)
                throw new ArgumentException("Đơn hàng đang ở trạng thái này rồi.");

            if (!IsValidStatusTransition(currentStatus, nextStatus))
                throw new InvalidOperationException($"Không thể chuyển trạng thái từ {currentStatus} sang {nextStatus}.");

            OrderDeliveryProof? deliveryProof = null;
            if (nextStatus == OrderStatusEnum.DaGiaoHang)
            {
                if (string.IsNullOrWhiteSpace(request.DeliveryProofImageUrl))
                    throw new ArgumentException("Vui lòng gửi ảnh minh chứng giao hàng thành công khi cập nhật đơn sang trạng thái Đã giao hàng.");

                deliveryProof = new OrderDeliveryProof
                {
                    OrderId = orderId,
                    ImageUrl = request.DeliveryProofImageUrl.Trim(),
                    Note = string.IsNullOrWhiteSpace(request.DeliveryProofNote)
                        ? "Đơn hàng đã giao thành công."
                        : request.DeliveryProofNote.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
            }
            else if (!string.IsNullOrWhiteSpace(request.DeliveryProofImageUrl))
            {
                throw new ArgumentException("Chỉ được gửi ảnh minh chứng khi cập nhật đơn sang trạng thái Đã giao hàng.");
            }

            var updatedOrder = await _orderRepository.UpdateStatusOrderAsync(orderId, newStatusId, deliveryProof);
            return MapToResponse(updatedOrder);
        }

        public async Task<OrderResponse> CancelMyOrderAsync(int orderId, int userId)
        {
            try
            {
                var order = await _orderRepository.CancelOrderAsync(
                    orderId,
                    userId,
                    userId,
                    "Khách hàng yêu cầu hủy đơn.",
                    CustomerCancelableStatusIds);

                return MapToResponse(order);
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi hủy đơn hàng: " + ex.Message);
            }
        }

        public async Task<OrderResponse> CancelOrderByAdminAsync(int orderId, int adminId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Vui lòng nhập lý do hủy đơn.");

            try
            {
                var order = await _orderRepository.CancelOrderAsync(
                    orderId,
                    null,
                    adminId,
                    reason,
                    GetAdminCancelableStatusIds());

                return MapToResponse(order);
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi Admin hủy đơn hàng: " + ex.Message);
            }
        }
    }
}
