using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response.Admin;

namespace MyOwnLearning.Interfaces
{
    public interface IAdminManagementRepository
    {
        Task<(List<AdminAuditLogResponse> Logs, int TotalCount)> GetAdminAuditLogsAsync(
            int? adminId,
            string? module,
            string? action,
            string? targetType,
            int? targetId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize);
        Task<AdminAuditLogResponse> LogAdminActionAsync(CreateAdminAuditLogRequest request);
        Task<AdminAlertSummaryResponse> GetAdminAlertSummaryAsync(
            int lowStockThreshold = 5,
            int voucherExpiringDays = 7,
            int lowRatingReviewDays = 7);
        Task<(List<SlowMovingProductResponse> Products, int TotalCount)> GetSlowMovingProductsAsync(
            int daysWithoutSale = 30,
            int page = 1,
            int pageSize = 10);
    }
}
