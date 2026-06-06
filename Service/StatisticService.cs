using MyOwnLearning.Interfaces;
using static MyOwnLearning.DTO.Request.Admin.Statistic.StatisticRequest;
using static MyOwnLearning.DTO.Response.Admin.Statistic.StatisticResponse;

namespace MyOwnLearning.Service
{
    public interface IStatisticService
    {
        Task<OverviewStatisticsResponse> GetOverviewAsync(StatisticsFilterRequest filter);
        Task<List<RevenueByCategory>> GetRevenueByCategoryAsync(StatisticsFilterRequest filter);
        Task<List<RevenueByBrand>> GetRevenueByBrandAsync(StatisticsFilterRequest filter);
        Task<List<RevenueByMonth>> GetRevenueByMonthAsync(int year);
        Task<List<TopProductResponse>> GetTopSellingProductsAsync(TopProductRequest request);
        Task<List<RevenueCategoryByMonth>> GetRevenueCategoryByMonthAsync(RevenueCategoryByMonthRequest request);
        Task<FullStatisticsReportResponse> GetFullReportAsync(StatisticsFilterRequest filter, int year);
        Task<List<OrderStatusStatisticResponse>> GetOrderStatusStatisticsAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<PaymentMethodStatisticResponse>> GetRevenueByPaymentMethodAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<VoucherStatisticResponse>> GetVoucherEffectivenessAsync(DateTime? fromDate, DateTime? toDate, int top = 10);
        Task<List<TopCustomerResponse>> GetTopCustomersAsync(DateTime? fromDate, DateTime? toDate, int top = 10);
    }

    public class StatisticService : IStatisticService
    {
        private readonly IStatisticRepository _statisticRepository;

        public StatisticService(IStatisticRepository statisticRepository)
        {
            _statisticRepository = statisticRepository;
        }

        // =====================================================
        // TỔNG QUAN (KPI CARDS)
        // =====================================================
        public async Task<OverviewStatisticsResponse> GetOverviewAsync(StatisticsFilterRequest filter)
        {
            return await _statisticRepository.GetOverviewAsync(filter);
        }

        // =====================================================
        // DOANH THU THEO DANH MỤC + tính tỷ trọng %
        // =====================================================
        public async Task<List<RevenueByCategory>> GetRevenueByCategoryAsync(StatisticsFilterRequest filter)
        {
            var categories = await _statisticRepository.GetRevenueByCategoryAsync(filter);

            var total = categories.Sum(c => c.TotalRevenue);
            if (total > 0)
            {
                foreach (var c in categories)
                {
                    c.RevenueShare = Math.Round((double)(c.TotalRevenue / total) * 100, 2);
                }
            }

            return categories;
        }

        // =====================================================
        // DOANH THU THEO THƯƠNG HIỆU + tính tỷ trọng %
        // =====================================================
        public async Task<List<RevenueByBrand>> GetRevenueByBrandAsync(StatisticsFilterRequest filter)
        {
            var brands = await _statisticRepository.GetRevenueByBrandAsync(filter);

            var total = brands.Sum(b => b.TotalRevenue);
            if (total > 0)
            {
                foreach (var b in brands)
                {
                    b.RevenueShare = Math.Round((double)(b.TotalRevenue / total) * 100, 2);
                }
            }

            return brands;
        }

        // =====================================================
        // DOANH THU THEO THÁNG + tính tăng trưởng MoM (%)
        // =====================================================
        public async Task<List<RevenueByMonth>> GetRevenueByMonthAsync(int year)
        {
            var months = await _statisticRepository.GetRevenueByMonthAsync(year);

            for (int i = 1; i < months.Count; i++)
            {
                var prev = months[i - 1].TotalRevenue;
                var curr = months[i].TotalRevenue;

                if (prev > 0)
                {
                    months[i].GrowthRate = Math.Round((double)((curr - prev) / prev) * 100, 2);
                }
                else if (curr > 0)
                {
                    months[i].GrowthRate = 100; // Tháng trước = 0, tháng này có doanh thu → tăng 100%
                }
                else
                {
                    months[i].GrowthRate = 0;
                }
            }

            return months;
        }

        // =====================================================
        // TOP SẢN PHẨM BÁN CHẠY
        // =====================================================
        public async Task<List<TopProductResponse>> GetTopSellingProductsAsync(TopProductRequest request)
        {
            if (request.Top <= 0 || request.Top > 100)
                throw new ArgumentException("Top phải nằm trong khoảng 1 đến 100.");

            return await _statisticRepository.GetTopSellingProductsAsync(request, request.Top);
        }

        // =====================================================
        // DOANH THU DANH MỤC THEO THÁNG (CROSS REPORT)
        // =====================================================
        public async Task<List<RevenueCategoryByMonth>> GetRevenueCategoryByMonthAsync(RevenueCategoryByMonthRequest request)
        {
            if (request.Year < 2000 || request.Year > DateTime.UtcNow.Year + 1)
                throw new ArgumentException("Năm không hợp lệ.");

            return await _statisticRepository.GetRevenueCategoryByMonthAsync(request.Year, request.CategoryId);
        }

        // =====================================================
        // FULL REPORT — gom tất cả cho Dashboard (1 request)
        // =====================================================
        public async Task<FullStatisticsReportResponse> GetFullReportAsync(StatisticsFilterRequest filter, int year)
        {
            // Gọi song song để tối ưu performance
            var overviewTask = _statisticRepository.GetOverviewAsync(filter);
            var categoryTask = _statisticRepository.GetRevenueByCategoryAsync(filter);
            var brandTask = _statisticRepository.GetRevenueByBrandAsync(filter);
            var monthTask = _statisticRepository.GetRevenueByMonthAsync(year);
            var topProductTask = _statisticRepository.GetTopSellingProductsAsync(filter, 10);
            var categoryMonthTask = _statisticRepository.GetRevenueCategoryByMonthAsync(year, null);

            await Task.WhenAll(overviewTask, categoryTask, brandTask, monthTask, topProductTask, categoryMonthTask);

            var categories = await categoryTask;
            var brands = await brandTask;
            var months = await monthTask;

            // Tính tỷ trọng danh mục
            var catTotal = categories.Sum(c => c.TotalRevenue);
            if (catTotal > 0)
                foreach (var c in categories)
                    c.RevenueShare = Math.Round((double)(c.TotalRevenue / catTotal) * 100, 2);

            // Tính tỷ trọng thương hiệu
            var brandTotal = brands.Sum(b => b.TotalRevenue);
            if (brandTotal > 0)
                foreach (var b in brands)
                    b.RevenueShare = Math.Round((double)(b.TotalRevenue / brandTotal) * 100, 2);

            // Tính tăng trưởng MoM
            for (int i = 1; i < months.Count; i++)
            {
                var prev = months[i - 1].TotalRevenue;
                var curr = months[i].TotalRevenue;
                months[i].GrowthRate = prev > 0
                    ? Math.Round((double)((curr - prev) / prev) * 100, 2)
                    : (curr > 0 ? 100 : 0);
            }

            return new FullStatisticsReportResponse
            {
                Overview = await overviewTask,
                RevenueByCategories = categories,
                RevenueByBrands = brands,
                RevenueByMonths = months,
                TopProducts = await topProductTask,
                RevenueCategoryMonthly = await categoryMonthTask
            };
        }

        public async Task<List<OrderStatusStatisticResponse>> GetOrderStatusStatisticsAsync(DateTime? fromDate, DateTime? toDate)
        {
            ValidateDateRange(fromDate, toDate);
            return await _statisticRepository.GetOrderStatusStatisticsAsync(fromDate, toDate);
        }

        public async Task<List<PaymentMethodStatisticResponse>> GetRevenueByPaymentMethodAsync(DateTime? fromDate, DateTime? toDate)
        {
            ValidateDateRange(fromDate, toDate);
            return await _statisticRepository.GetRevenueByPaymentMethodAsync(fromDate, toDate);
        }

        public async Task<List<VoucherStatisticResponse>> GetVoucherEffectivenessAsync(DateTime? fromDate, DateTime? toDate, int top = 10)
        {
            ValidateDateRange(fromDate, toDate);
            ValidateTop(top);
            return await _statisticRepository.GetVoucherEffectivenessAsync(fromDate, toDate, top);
        }

        public async Task<List<TopCustomerResponse>> GetTopCustomersAsync(DateTime? fromDate, DateTime? toDate, int top = 10)
        {
            ValidateDateRange(fromDate, toDate);
            ValidateTop(top);
            return await _statisticRepository.GetTopCustomersAsync(fromDate, toDate, top);
        }

        private static void ValidateDateRange(DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            {
                throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            }
        }

        private static void ValidateTop(int top)
        {
            if (top <= 0 || top > 100)
            {
                throw new ArgumentException("Top phải nằm trong khoảng 1 đến 100.");
            }
        }
    }
}
