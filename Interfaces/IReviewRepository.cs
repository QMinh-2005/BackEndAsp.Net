using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<bool> HasUserReviewedProductAsync(int userId, int productId);
        Task<OrderDetail?> GetReviewableOrderDetailAsync(int userId, int orderDetailId);
        Task<Review?> GetReviewByIdWithDetailsAsync(int reviewId);
        Task<Review?> GetReviewByOrderDetailIdAsync(int orderDetailId);
        Task<(List<Review> Reviews, int TotalCount)> GetReviewsByProductIdAsync(int productId, int page, int pageSize, bool includeHidden = false);
        Task<(List<Review> Reviews, int TotalCount)> GetReviewsByUserIdAsync(int userId, int page, int pageSize);
        Task<(List<Review> Reviews, int TotalCount)> GetReviewsForAdminAsync(
            int page,
            int pageSize,
            bool? isVisible,
            int? rating,
            int? productId,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate);
        Task<(List<OrderDetail> OrderDetails, int TotalCount)> GetReviewableOrderDetailsByUserIdAsync(int userId, int page, int pageSize);
        Task<double> GetAverageRatingByProductIdAsync(int productId);
    }
}
