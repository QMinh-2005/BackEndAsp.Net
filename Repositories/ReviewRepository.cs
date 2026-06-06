using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class ReviewRepository : Repository<Review>, IReviewRepository
    {
        public ReviewRepository(WebBadmintonContext context) : base(context)
        {
        }

        public async Task<bool> HasUserReviewedProductAsync(int userId, int productId)
        {
            return await _dbset.AnyAsync(r =>
                r.OrderDetail.Order.UserId == userId &&
                r.OrderDetail.Detail != null &&
                r.OrderDetail.Detail.ProductId == productId);
        }

        public async Task<OrderDetail?> GetReviewableOrderDetailAsync(int userId, int orderDetailId)
        {
            return await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Detail)
                    .ThenInclude(d => d.Product)
                .Include(od => od.Review)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId && od.Order != null && od.Order.UserId == userId);
        }

        public async Task<Review?> GetReviewByIdWithDetailsAsync(int reviewId)
        {
            return await BaseReviewQuery()
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);
        }

        public async Task<Review?> GetReviewByOrderDetailIdAsync(int orderDetailId)
        {
            return await BaseReviewQuery()
                .FirstOrDefaultAsync(r => r.OrderDetailId == orderDetailId);
        }

        public async Task<(List<Review> Reviews, int TotalCount)> GetReviewsByProductIdAsync(
            int productId,
            int page,
            int pageSize,
            bool includeHidden = false)
        {
            var query = BaseReviewQuery()
                .Where(r => r.OrderDetail.Detail != null && r.OrderDetail.Detail.ProductId == productId);

            if (!includeHidden)
            {
                query = query.Where(r => r.IsVisible);
            }

            var totalCount = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.ReviewDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (reviews, totalCount);
        }

        public async Task<(List<Review> Reviews, int TotalCount)> GetReviewsByUserIdAsync(int userId, int page, int pageSize)
        {
            var query = BaseReviewQuery()
                .Where(r => r.OrderDetail.Order != null && r.OrderDetail.Order.UserId == userId);

            var totalCount = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.ReviewDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (reviews, totalCount);
        }

        public async Task<(List<Review> Reviews, int TotalCount)> GetReviewsForAdminAsync(
            int page,
            int pageSize,
            bool? isVisible,
            int? rating,
            int? productId,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = BaseReviewQuery().AsNoTracking();

            if (isVisible.HasValue)
            {
                query = query.Where(r => r.IsVisible == isVisible.Value);
            }

            if (rating.HasValue)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }

            if (productId.HasValue)
            {
                query = query.Where(r =>
                    r.OrderDetail.Detail != null &&
                    r.OrderDetail.Detail.ProductId == productId.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(r =>
                    r.OrderDetail.Order != null &&
                    r.OrderDetail.Order.UserId == userId.Value);
            }

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;
                query = query.Where(r => r.ReviewDate >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);
                query = query.Where(r => r.ReviewDate < endDate);
            }

            var totalCount = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.ReviewDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (reviews, totalCount);
        }

        public async Task<(List<OrderDetail> OrderDetails, int TotalCount)> GetReviewableOrderDetailsByUserIdAsync(
            int userId,
            int page,
            int pageSize)
        {
            var query = _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Review)
                .Include(od => od.Detail)
                    .ThenInclude(d => d.Product)
                .Where(od =>
                    od.Order != null &&
                    od.Order.UserId == userId &&
                    (od.Order.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                     od.Order.OrderStatusId == (int)OrderStatusEnum.HoanTat) &&
                    od.Review == null);

            var totalCount = await query.CountAsync();
            var orderDetails = await query
                .OrderByDescending(od => od.Order!.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orderDetails, totalCount);
        }

        public async Task<double> GetAverageRatingByProductIdAsync(int productId)
        {
            var query = _dbset
                .Where(r => r.IsVisible &&
                            r.OrderDetail.Detail != null &&
                            r.OrderDetail.Detail.ProductId == productId);

            if (!await query.AnyAsync())
            {
                return 0;
            }

            return await query.AverageAsync(r => r.Rating);
        }

        private IQueryable<Review> BaseReviewQuery()
        {
            return _dbset
                .Include(r => r.ReviewImages)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Order)
                        .ThenInclude(o => o.User)
                            .ThenInclude(u => u.UserProfiles)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Detail)
                        .ThenInclude(d => d.Product)
                .AsSplitQuery();
        }
    }
}
