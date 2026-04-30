using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.Interfaces;

namespace MyOwnLearning.Service
{
    public interface IOrderService
    {
        Task<List<OrderResponse>> GetAllOrdersAsync();
        Task<List<OrderResponse>> GetMyOrdersAsync(int userId);
        Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request);
        //Task<OrderResponse> UpdateOrderStatusAsync(int orderId, string NewStatus);
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
        public async Task<List<OrderResponse>> GetMyOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);

            // Chuyển đổi dữ liệu từ Entity sang DTO
            var orderResponses = orders.Select(o => new OrderResponse
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
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
        public async Task<List<OrderResponse>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllOrdersWithDetailsAsync();
            // Chuyển đổi dữ liệu từ Entity sang DTO
            var orderResponses = orders.Select(o => new OrderResponse
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
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
                    Status = order.Status,
                    ShippingAddress = order.ShippingAddress,
                    ShippingFee = order.ShippingFee,
                    Note = order.Note,
                    ReceiverName = order.ReceiverName,
                    PhoneNumber = order.PhoneNumber,
                    PaymentMethod = order.Payment?.PaymentMethod ?? "Chưa xác định",
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

    }
}
