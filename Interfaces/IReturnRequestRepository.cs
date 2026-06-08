using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IReturnRequestRepository : IRepository<ReturnRequest>
    {
        Task<bool> IsOrderOwnedByUserAsync(int orderId, int userId);
        Task<bool> IsOrderReturnableAsync(int orderId, int userId);
        Task<Order?> GetReturnableOrderForUserAsync(int orderId, int userId);
        Task<bool> HasOpenReturnRequestAsync(int orderId, int userId);
        Task<bool> HasDeliveryProofsAsync(int orderId);
        Task<bool> CanAddDeliveryProofAsync(int orderId);
        Task<ReturnRequest?> GetReturnRequestByIdAsync(int returnRequestId);
        Task<ReturnRequest?> GetReturnRequestByIdForUserAsync(int returnRequestId, int userId);
        Task<ReturnRequest> CreateReturnRequestAsync(ReturnRequest request, int newOrderStatusId);
        Task<(List<ReturnRequest> Requests, int TotalCount)> GetReturnRequestsByUserAsync(int userId, int page, int pageSize);
        Task<(List<ReturnRequest> Requests, int TotalCount)> GetReturnRequestsForAdminAsync(
            string? status,
            int? orderId,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize);
        Task<List<OrderDeliveryProof>> GetDeliveryProofsByOrderIdAsync(int orderId);
        Task<OrderDeliveryProof> AddDeliveryProofAsync(OrderDeliveryProof proof);
        Task<ReturnRequest> UpdateReturnRequestAsync(ReturnRequest request, int? newOrderStatusId = null, bool restoreOriginalOrderStatus = false);
    }
}
