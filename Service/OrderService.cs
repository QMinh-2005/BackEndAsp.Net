using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;

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
    }
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly WebBadmintonContext _context;
        public OrderService(IOrderRepository orderRepository, ICartRepository cartRepository, WebBadmintonContext context)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _context = context;
        }
        private static readonly Dictionary<OrderStatusEnum, List<OrderStatusEnum>> _validTransitions = new()
        {
            // Chờ xác nhận -> có thể Xác nhận hoặc Hủy
            { OrderStatusEnum.ChoXacNhan, new List<OrderStatusEnum> { OrderStatusEnum.DaXacNhan, OrderStatusEnum.DaHuy } },
        
            // Đã xác nhận -> có thể đem đi Đóng gói, Đan lưới, hoặc Hủy
            { OrderStatusEnum.DaXacNhan, new List<OrderStatusEnum> { OrderStatusEnum.DangXuLy, OrderStatusEnum.DangDanLuoi, OrderStatusEnum.DaHuy } },
        
            // Đang xử lý -> có thể chuyển sang Giao hàng (hoặc Hủy nếu chưa đưa cho shipper)
            { OrderStatusEnum.DangXuLy, new List<OrderStatusEnum> { OrderStatusEnum.DangGiaoHang, OrderStatusEnum.DaHuy } },
        
            // Đang đan lưới -> đan xong có thể về Đang xử lý (để đóng gói chung món khác) hoặc Giao hàng luôn. KHÔNG CHO HỦY VÌ ĐÃ CẮT CƯỚC.
            { OrderStatusEnum.DangDanLuoi, new List<OrderStatusEnum> { OrderStatusEnum.DangXuLy, OrderStatusEnum.DangGiaoHang } },
        
            // Đang giao hàng -> có thể Đã giao hoặc Hủy (nếu khách bom hàng / giao thất bại)
            { OrderStatusEnum.DangGiaoHang, new List<OrderStatusEnum> { OrderStatusEnum.DaGiaoHang, OrderStatusEnum.DaHuy } },
        
            // Đã giao hàng -> Hoàn tất
            { OrderStatusEnum.DaGiaoHang, new List<OrderStatusEnum> { OrderStatusEnum.HoanTat } },
        
            // Hoàn tất và Đã hủy là trạng thái cuối, không đi đâu nữa
            { OrderStatusEnum.HoanTat, new List<OrderStatusEnum>() },
            { OrderStatusEnum.DaHuy, new List<OrderStatusEnum>() }
        };
        private bool IsValidStatusTransition(OrderStatusEnum currentStatus, OrderStatusEnum newStatus)
        {
            return _validTransitions.ContainsKey(currentStatus) && _validTransitions[currentStatus].Contains(newStatus);
        }
        public async Task<List<OrderResponse>> GetMyOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);

            // Chuyển đổi dữ liệu từ Entity sang DTO
            var orderResponses = orders.Select(o => new OrderResponse
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.OrderStatus?.StatusName ?? "Chưa xác định",
                ShippingFee = o.ShippingFee,
                ReceiverName = o.ReceiverName,
                PhoneNumber = o.PhoneNumber,
                ShippingAddress = o.ShippingAddress,
                Note = o.Note,
                PaymentMethod = o.Payment?.PaymentMethod ?? "Chưa xác định",
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailResponse
                {
                    OrderDetailId = od.OrderDetailId,
                    DetailId = od.DetailId,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    IsStringingService = od.IsStringingService,
                    StringBrand = od.StringBrand,
                    TensionKg = od.TensionKg,
                    ProductName = od.Detail?.Product?.ProductName
                }).ToList()
            }).ToList();

            return orderResponses;
        }
        public async Task<(List<OrderResponse> Orders, int TotalCount)> GetAllOrdersAsync(int page, int pageSize)
        {
            var (orders, totalCount) = await _orderRepository.GetAllOrdersWithDetailsAsync(page, pageSize);
            // Chuyển đổi dữ liệu từ Entity sang DTO
            var orderResponses = orders.Select(o => new OrderResponse
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.OrderStatus?.StatusName ?? "Chưa xác định",
                ShippingFee = o.ShippingFee,
                ReceiverName = o.ReceiverName,
                PhoneNumber = o.PhoneNumber,
                ShippingAddress = o.ShippingAddress,
                Note = o.Note,
                PaymentMethod = o.Payment?.PaymentMethod ?? "Chưa xác định",
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailResponse
                {
                    OrderDetailId = od.OrderDetailId,
                    DetailId = od.DetailId,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    IsStringingService = od.IsStringingService,
                    StringBrand = od.StringBrand,
                    TensionKg = od.TensionKg,
                    ProductName = od.Detail?.Product?.ProductName
                }).ToList()
            }).ToList();
            return (orderResponses, totalCount);
        }
        public async Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.CreateOrderAsync(userId, request);
                await transaction.CommitAsync();
                return new OrderResponse
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    ShippingAddress = order.ShippingAddress,
                    ShippingFee = order.ShippingFee,
                    Note = order.Note,
                    ReceiverName = order.ReceiverName,
                    PhoneNumber = order.PhoneNumber,
                    PaymentMethod = order.Payment?.PaymentMethod ?? "Chưa xác định",
                    Status = order.OrderStatus?.StatusName ?? "Chưa xác định",
                    OrderDetails = order.OrderDetails.Select(od => new OrderDetailResponse
                    {
                        OrderDetailId = od.OrderDetailId,
                        DetailId = od.DetailId,
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        IsStringingService = od.IsStringingService,
                        StringBrand = od.StringBrand,
                        TensionKg = od.TensionKg,
                        ProductName = od.Detail?.Product?.ProductName
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Đã xảy ra lỗi khi tạo đơn hàng: " + ex.Message);
            }
        }
        public async Task<OrderResponse> UpdateOrderStatusAsync(int orderId, int newStatusId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId); // Giả sử bạn có hàm GetById
            if (order == null)
            {
                throw new Exception("Không tìm thấy đơn hàng.");
            }
            if (!Enum.IsDefined(typeof(OrderStatusEnum), newStatusId))
            {
                throw new ArgumentException("Trạng thái mới không hợp lệ.");
            }
            var currentStatus = (OrderStatusEnum)order.OrderStatusId;
            var nextStatus = (OrderStatusEnum)newStatusId;

            // Chặn nếu trạng thái mới giống trạng thái hiện tại
            if (currentStatus == nextStatus)
            {
                throw new ArgumentException("Đơn hàng đang ở trạng thái này rồi.");
            }
            if (!IsValidStatusTransition(currentStatus, nextStatus))
            {
                throw new InvalidOperationException($"Không thể chuyển trạng thái từ {currentStatus} sang {nextStatus}.");
            }
            order.OrderStatusId = newStatusId;
            await _orderRepository.UpdateAsync(order);
            await _context.Entry(order).Reference(o => o.OrderStatus).LoadAsync();
            return new OrderResponse
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                ShippingAddress = order.ShippingAddress,
                ShippingFee = order.ShippingFee,
                Note = order.Note,
                ReceiverName = order.ReceiverName,
                PhoneNumber = order.PhoneNumber,
                PaymentMethod = order.Payment?.PaymentMethod ?? "Chưa xác định",
                Status = order.OrderStatus?.StatusName ?? "Chưa xác định",
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailResponse
                {
                    OrderDetailId = od.OrderDetailId,
                    DetailId = od.DetailId,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    IsStringingService = od.IsStringingService,
                    StringBrand = od.StringBrand,
                    TensionKg = od.TensionKg,
                    ProductName = od.Detail?.Product?.ProductName
                }).ToList()
            };
        }
        public async Task<OrderResponse> CancelMyOrderAsync(int orderId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _orderRepository.GetOrderByIdAndUserIdAsync(orderId, userId);
                if (order == null)
                {
                    throw new Exception("Không tìm thấy đơn hàng hoặc bạn không có quyền hủy đơn hàng này.");
                }
                if (order.OrderStatusId == (int)OrderStatusEnum.DaHuy)
                {
                    throw new InvalidOperationException("Đơn hàng đã được hủy trước đó.");
                }
                if (order.OrderStatusId != (int)OrderStatusEnum.ChoXacNhan && order.OrderStatusId != (int)OrderStatusEnum.DaXacNhan)
                {
                    throw new InvalidOperationException("Không thể hủy đơn hàng");
                }
                foreach (var OrderDetail in order.OrderDetails)
                {
                    if (OrderDetail.Detail != null)
                    {
                        OrderDetail.Detail.StockQuantity += OrderDetail.Quantity;
                        _context.ProductDetails.Update(OrderDetail.Detail);
                    }
                }
                order.OrderStatusId = (int)OrderStatusEnum.DaHuy;
                await _orderRepository.UpdateAsync(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await _context.Entry(order).Reference(o => o.OrderStatus).LoadAsync();
                return new OrderResponse
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    ShippingAddress = order.ShippingAddress,
                    ShippingFee = order.ShippingFee,
                    Note = order.Note,
                    ReceiverName = order.ReceiverName,
                    PhoneNumber = order.PhoneNumber,
                    PaymentMethod = order.Payment?.PaymentMethod ?? "Chưa xác định",
                    Status = order.OrderStatus?.StatusName ?? "Đã hủy",
                    OrderDetails = order.OrderDetails.Select(od => new OrderDetailResponse
                    {
                        OrderDetailId = od.OrderDetailId,
                        DetailId = od.DetailId,
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        IsStringingService = od.IsStringingService,
                        StringBrand = od.StringBrand,
                        TensionKg = od.TensionKg,
                        ProductName = od.Detail?.Product?.ProductName
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Đã xảy ra lỗi khi hủy đơn hàng: " + ex.Message);
            }
        }
        public async Task<(List<OrderResponse> Orders, int TotalCount)> GetOrdersByStatusIdAsync(int statusId, int page, int pageSize)
        {
            var (orders, totalCount) = await _orderRepository.GetOrdersByStatusIdAsync(statusId, page, pageSize);
            var orderResponses = orders.Select(o => new OrderResponse
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                ShippingAddress = o.ShippingAddress,
                ShippingFee = o.ShippingFee,
                Note = o.Note,
                ReceiverName = o.ReceiverName,
                PhoneNumber = o.PhoneNumber,
                PaymentMethod = o.Payment?.PaymentMethod ?? "Chưa xác định",
                Status = o.OrderStatus?.StatusName ?? "Chưa xác định",
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailResponse
                {
                    OrderDetailId = od.OrderDetailId,
                    DetailId = od.DetailId,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    IsStringingService = od.IsStringingService,
                    StringBrand = od.StringBrand,
                    TensionKg = od.TensionKg,
                    ProductName = od.Detail?.Product?.ProductName
                }).ToList()
            }).ToList();
            return (orderResponses, totalCount);
        }
    }
}