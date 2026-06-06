using static MyOwnLearning.DTO.Request.Admin.Statistic.StatisticRequest;
using static MyOwnLearning.DTO.Response.Admin.Statistic.StatisticResponse;

namespace MyOwnLearning.Interfaces
{
    public interface IStatisticRepository
    {
        Task<List<RevenueByCategory>> GetRevenueByCategoryAsync(StatisticsFilterRequest filter);
        Task<List<RevenueByBrand>> GetRevenueByBrandAsync(StatisticsFilterRequest filter);
        Task<List<RevenueByMonth>> GetRevenueByMonthAsync(int year);
        Task<List<TopProductResponse>> GetTopSellingProductsAsync(StatisticsFilterRequest filter, int top = 10);
        Task<OverviewStatisticsResponse> GetOverviewAsync(StatisticsFilterRequest filter);
        Task<List<RevenueCategoryByMonth>> GetRevenueCategoryByMonthAsync(int year, int? categoryId);
        Task<List<OrderStatusStatisticResponse>> GetOrderStatusStatisticsAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<PaymentMethodStatisticResponse>> GetRevenueByPaymentMethodAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<VoucherStatisticResponse>> GetVoucherEffectivenessAsync(DateTime? fromDate, DateTime? toDate, int top = 10);
        Task<List<TopCustomerResponse>> GetTopCustomersAsync(DateTime? fromDate, DateTime? toDate, int top = 10);
    }
}
