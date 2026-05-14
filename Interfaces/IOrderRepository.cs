using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.Models;
using MyOwnLearning.Service;

namespace MyOwnLearning.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {

        Task<List<Order>> GetOrdersByUserIdAsync(int userId);
        Task<(List<Order> Orders, int TotalCount)> GetAllOrdersWithDetailsAsync(int page, int pageSize);
        Task<Order> CreateOrderAsync(int userId, CreateOrderRequest request, List<AppliedVoucherDetail> voucherDetails);
        Task<Order> GetOrderByIdAsync(int orderId);
        Task<Order> UpdateStatusOrderAsync(int orderId, int newStatusId);
        Task<Order> GetOrderByIdAndUserIdAsync(int orderId, int userId);
        Task<(List<Order> Orders, int TotalCount)> GetOrdersByStatusIdAsync(int statusId, int page, int pageSize);
        Task<(List<Order> Orders, int TotalCount)> SearchOrderAdminAsync(decimal? minPrice, decimal? maxPrice, DateTime? orderDate, int? statusId, int page, int pageSize);
        Task<int> CountSuccessfulUsesAsync(int userId, int voucherId);
    }
}
