using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Service
{
    public interface IReviewService
    {
        Task<bool> HasUserReviewedProductAsync(int userId, int productId);
        Task<(List<ReviewResponse> Reviews, double AverageRating, int TotalCount)> GetReviewsByProductIdAsync(int productId, int page, int pageSize);
        Task<(List<ReviewResponse> Reviews, int TotalCount)> GetMyReviewsAsync(int userId, int page, int pageSize);
        Task<(List<ReviewableOrderDetailResponse> Items, int TotalCount)> GetMyReviewableOrderDetailsAsync(int userId, int page, int pageSize);
        Task<(List<ReviewResponse> Reviews, int TotalCount)> GetReviewsForAdminAsync(
            int page,
            int pageSize,
            bool? isVisible,
            int? rating,
            int? productId,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate);
        Task<ReviewResponse> CreateReviewAsync(int userId, CreateReviewRequest request);
        Task<ReviewResponse> UpdateMyReviewAsync(int userId, int reviewId, UpdateReviewRequest request);
        Task<bool> DeleteMyReviewAsync(int userId, int reviewId);
        Task<ReviewResponse> SetReviewVisibilityAsync(int reviewId, bool isVisible);
    }

    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;

        public ReviewService(IReviewRepository reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        public async Task<bool> HasUserReviewedProductAsync(int userId, int productId)
        {
            return await _reviewRepository.HasUserReviewedProductAsync(userId, productId);
        }

        public async Task<(List<ReviewResponse> Reviews, double AverageRating, int TotalCount)> GetReviewsByProductIdAsync(
            int productId,
            int page,
            int pageSize)
        {
            ValidatePaging(page, pageSize);

            var (reviews, totalCount) = await _reviewRepository.GetReviewsByProductIdAsync(productId, page, pageSize);
            var averageRating = await _reviewRepository.GetAverageRatingByProductIdAsync(productId);

            return (reviews.Select(MapToResponse).ToList(), averageRating, totalCount);
        }

        public async Task<(List<ReviewResponse> Reviews, int TotalCount)> GetMyReviewsAsync(int userId, int page, int pageSize)
        {
            ValidatePaging(page, pageSize);

            var (reviews, totalCount) = await _reviewRepository.GetReviewsByUserIdAsync(userId, page, pageSize);
            return (reviews.Select(MapToResponse).ToList(), totalCount);
        }

        public async Task<(List<ReviewableOrderDetailResponse> Items, int TotalCount)> GetMyReviewableOrderDetailsAsync(
            int userId,
            int page,
            int pageSize)
        {
            ValidatePaging(page, pageSize);

            var (orderDetails, totalCount) = await _reviewRepository.GetReviewableOrderDetailsByUserIdAsync(userId, page, pageSize);
            return (orderDetails.Select(MapToReviewableResponse).ToList(), totalCount);
        }

        public async Task<(List<ReviewResponse> Reviews, int TotalCount)> GetReviewsForAdminAsync(
            int page,
            int pageSize,
            bool? isVisible,
            int? rating,
            int? productId,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            ValidatePaging(page, pageSize);

            if (rating.HasValue && (rating.Value < 1 || rating.Value > 5))
                throw new ArgumentException("Rating phải nằm trong khoảng 1 đến 5.");

            var (reviews, totalCount) = await _reviewRepository.GetReviewsForAdminAsync(
                page,
                pageSize,
                isVisible,
                rating,
                productId,
                userId,
                fromDate,
                toDate);
            return (reviews.Select(MapToResponse).ToList(), totalCount);
        }

        public async Task<ReviewResponse> CreateReviewAsync(int userId, CreateReviewRequest request)
        {
            var orderDetail = await _reviewRepository.GetReviewableOrderDetailAsync(userId, request.OrderDetailId);
            if (orderDetail == null)
                throw new UnauthorizedAccessException("Bạn không có quyền đánh giá sản phẩm này.");

            if (orderDetail.Detail == null)
                throw new InvalidOperationException("Không tìm thấy sản phẩm cần đánh giá.");

            if (!CanReviewOrder(orderDetail.Order?.OrderStatusId))
                throw new InvalidOperationException("Bạn chỉ có thể đánh giá sau khi đơn hàng đã giao hoặc hoàn tất.");

            if (orderDetail.Review != null)
                throw new InvalidOperationException("Sản phẩm trong đơn hàng này đã được đánh giá.");

            var review = new Review
            {
                OrderDetailId = request.OrderDetailId,
                Rating = request.Rating,
                Comment = NormalizeComment(request.Comment),
                ReviewDate = DateTime.UtcNow,
                IsVisible = true,
                ReviewImages = BuildReviewImages(request.ImageUrls)
            };

            await _reviewRepository.AddAsync(review);

            var createdReview = await _reviewRepository.GetReviewByIdWithDetailsAsync(review.ReviewId);
            return MapToResponse(createdReview ?? review);
        }

        public async Task<ReviewResponse> UpdateMyReviewAsync(int userId, int reviewId, UpdateReviewRequest request)
        {
            var review = await GetOwnedReviewAsync(userId, reviewId);

            if (request.Rating.HasValue)
                review.Rating = request.Rating.Value;

            if (request.Comment != null)
                review.Comment = NormalizeComment(request.Comment);

            if (request.ImageUrls != null)
            {
                review.ReviewImages.Clear();
                foreach (var image in BuildReviewImages(request.ImageUrls))
                {
                    review.ReviewImages.Add(image);
                }
            }

            review.UpdatedAt = DateTime.UtcNow;
            await _reviewRepository.UpdateAsync(review);

            var updatedReview = await _reviewRepository.GetReviewByIdWithDetailsAsync(reviewId);
            return MapToResponse(updatedReview ?? review);
        }

        public async Task<bool> DeleteMyReviewAsync(int userId, int reviewId)
        {
            var review = await GetOwnedReviewAsync(userId, reviewId);
            await _reviewRepository.DeleteAsync(review.ReviewId);
            return true;
        }

        public async Task<ReviewResponse> SetReviewVisibilityAsync(int reviewId, bool isVisible)
        {
            var review = await _reviewRepository.GetReviewByIdWithDetailsAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException("Không tìm thấy đánh giá.");

            review.IsVisible = isVisible;
            review.UpdatedAt = DateTime.UtcNow;
            await _reviewRepository.UpdateAsync(review);

            return MapToResponse(review);
        }
        private async Task<Review> GetOwnedReviewAsync(int userId, int reviewId)
        {
            var review = await _reviewRepository.GetReviewByIdWithDetailsAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException("Không tìm thấy đánh giá.");

            if (review.OrderDetail.Order?.UserId != userId)
                throw new UnauthorizedAccessException("Bạn chỉ được thao tác với đánh giá của chính mình.");

            return review;
        }

        private static bool CanReviewOrder(int? orderStatusId)
        {
            return orderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                   orderStatusId == (int)OrderStatusEnum.HoanTat;
        }

        private static string? NormalizeComment(string? comment)
        {
            return string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        }

        private static List<ReviewImage> BuildReviewImages(List<string>? imageUrls)
        {
            if (imageUrls == null)
                return new List<ReviewImage>();

            return imageUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select((url, index) => new ReviewImage
                {
                    ImageUrl = url.Trim(),
                    DisplayOrder = index + 1
                })
                .ToList();
        }

        private static ReviewResponse MapToResponse(Review review)
        {
            var orderDetail = review.OrderDetail;
            var product = orderDetail.Detail?.Product;
            var user = orderDetail.Order?.User;
            var userName = user?.UserProfiles.FirstOrDefault()?.FullName;

            return new ReviewResponse
            {
                ReviewId = review.ReviewId,
                OrderDetailId = review.OrderDetailId,
                ProductId = product?.ProductId,
                ProductName = product?.ProductName ?? string.Empty,
                ProductImageUrl = product?.MainImageUrl,
                UserId = user?.UserId,
                UserName = !string.IsNullOrWhiteSpace(userName) ? userName : user?.Email ?? string.Empty,
                Rating = review.Rating,
                Comment = review.Comment,
                ReviewDate = review.ReviewDate,
                UpdatedAt = review.UpdatedAt,
                IsVisible = review.IsVisible,
                Images = review.ReviewImages
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .ToList()
            };
        }

        private static ReviewableOrderDetailResponse MapToReviewableResponse(OrderDetail orderDetail)
        {
            var product = orderDetail.Detail?.Product;
            var variantParts = new[]
            {
                orderDetail.Detail?.WeightClass,
                orderDetail.Detail?.GripSize,
                orderDetail.Detail?.BalancePoint,
                orderDetail.Detail?.Stiffness
            }.Where(x => !string.IsNullOrWhiteSpace(x));

            return new ReviewableOrderDetailResponse
            {
                OrderDetailId = orderDetail.OrderDetailId,
                OrderId = orderDetail.OrderId,
                OrderDate = orderDetail.Order?.OrderDate,
                ProductId = product?.ProductId,
                ProductName = product?.ProductName ?? string.Empty,
                ProductImageUrl = product?.MainImageUrl,
                VariantInfo = string.Join(" - ", variantParts),
                Quantity = orderDetail.Quantity,
                UnitPrice = orderDetail.UnitPrice
            };
        }

        private static void ValidatePaging(int page, int pageSize)
        {
            if (page <= 0)
                throw new ArgumentException("Page phải lớn hơn 0.");

            if (pageSize <= 0 || pageSize > 100)
                throw new ArgumentException("PageSize phải nằm trong khoảng 1 đến 100.");
        }
    }
}
