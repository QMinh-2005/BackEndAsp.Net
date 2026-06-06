using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Interfaces;

namespace MyOwnLearning.Service
{
    public interface IAdminManagementService
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

    public class AdminManagementService : IAdminManagementService
    {
        private readonly IAdminManagementRepository _adminManagementRepository;

        public AdminManagementService(IAdminManagementRepository adminManagementRepository)
        {
            _adminManagementRepository = adminManagementRepository;
        }

        public async Task<(List<AdminAuditLogResponse> Logs, int TotalCount)> GetAdminAuditLogsAsync(
            int? adminId,
            string? module,
            string? action,
            string? targetType,
            int? targetId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            NormalizePaging(ref page, ref pageSize);
            ValidateDateRange(fromDate, toDate);

            return await _adminManagementRepository.GetAdminAuditLogsAsync(
                adminId,
                module,
                action,
                targetType,
                targetId,
                fromDate,
                toDate,
                page,
                pageSize);
        }

        public async Task<AdminAuditLogResponse> LogAdminActionAsync(CreateAdminAuditLogRequest request)
        {
            if (request == null)
                throw new ArgumentException("Log request is required.");

            if (string.IsNullOrWhiteSpace(request.Module))
                throw new ArgumentException("Module is required.");

            if (string.IsNullOrWhiteSpace(request.Action))
                throw new ArgumentException("Action is required.");

            if (string.IsNullOrWhiteSpace(request.TargetType))
                throw new ArgumentException("TargetType is required.");

            return await _adminManagementRepository.LogAdminActionAsync(request);
        }

        public async Task<AdminAlertSummaryResponse> GetAdminAlertSummaryAsync(
            int lowStockThreshold = 5,
            int voucherExpiringDays = 7,
            int lowRatingReviewDays = 7)
        {
            if (lowStockThreshold < 0) lowStockThreshold = 0;
            if (voucherExpiringDays < 1) voucherExpiringDays = 1;
            if (lowRatingReviewDays < 1) lowRatingReviewDays = 1;

            return await _adminManagementRepository.GetAdminAlertSummaryAsync(
                lowStockThreshold,
                voucherExpiringDays,
                lowRatingReviewDays);
        }

        public async Task<(List<SlowMovingProductResponse> Products, int TotalCount)> GetSlowMovingProductsAsync(
            int daysWithoutSale = 30,
            int page = 1,
            int pageSize = 10)
        {
            if (daysWithoutSale < 1) daysWithoutSale = 30;
            NormalizePaging(ref page, ref pageSize);

            return await _adminManagementRepository.GetSlowMovingProductsAsync(daysWithoutSale, page, pageSize);
        }

        private static void NormalizePaging(ref int page, ref int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
        }

        private static void ValidateDateRange(DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                throw new ArgumentException("FromDate must be less than or equal to ToDate.");
        }
    }
}
