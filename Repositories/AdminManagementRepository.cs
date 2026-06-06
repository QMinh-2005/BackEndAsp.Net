using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class AdminManagementRepository : IAdminManagementRepository
    {
        private static readonly string[] PaidPaymentStatuses =
        {
            "Paid",
            "Completed",
            "Success",
            "Succeeded",
            "Da thanh toan"
        };

        private static readonly string[] ClosedServiceTicketStatuses =
        {
            "Hoan thanh",
            "Da hoan thanh",
            "Completed",
            "Done",
            "Cancelled",
            "Da huy"
        };

        private readonly WebBadmintonContext _context;

        public AdminManagementRepository(WebBadmintonContext context)
        {
            _context = context;
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
            var query = _context.AdminAuditLogs.AsNoTracking().AsQueryable();

            if (adminId.HasValue)
                query = query.Where(l => l.AdminId == adminId.Value);

            if (!string.IsNullOrWhiteSpace(module))
            {
                var trimmedModule = module.Trim();
                query = query.Where(l => l.Module == trimmedModule);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                var trimmedAction = action.Trim();
                query = query.Where(l => l.Action == trimmedAction);
            }

            if (!string.IsNullOrWhiteSpace(targetType))
            {
                var trimmedTargetType = targetType.Trim();
                query = query.Where(l => l.TargetType == trimmedTargetType);
            }

            if (targetId.HasValue)
                query = query.Where(l => l.TargetId == targetId.Value);

            query = ApplyAuditDateFilter(query, fromDate, toDate);

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new AdminAuditLogResponse
                {
                    AuditLogId = l.AuditLogId,
                    AdminId = l.AdminId,
                    AdminEmail = l.AdminEmail,
                    Module = l.Module,
                    Action = l.Action,
                    TargetType = l.TargetType,
                    TargetId = l.TargetId,
                    Description = l.Description,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return (logs, totalCount);
        }

        public async Task<AdminAuditLogResponse> LogAdminActionAsync(CreateAdminAuditLogRequest request)
        {
            var log = new AdminAuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                Module = request.Module.Trim(),
                Action = request.Action.Trim(),
                TargetType = request.TargetType.Trim(),
                TargetId = request.TargetId,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminAuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return new AdminAuditLogResponse
            {
                AuditLogId = log.AuditLogId,
                AdminId = log.AdminId,
                AdminEmail = log.AdminEmail,
                Module = log.Module,
                Action = log.Action,
                TargetType = log.TargetType,
                TargetId = log.TargetId,
                Description = log.Description,
                CreatedAt = log.CreatedAt
            };
        }

        public async Task<AdminAlertSummaryResponse> GetAdminAlertSummaryAsync(
            int lowStockThreshold = 5,
            int voucherExpiringDays = 7,
            int lowRatingReviewDays = 7)
        {
            var now = DateTime.UtcNow;
            var voucherExpireBefore = now.Date.AddDays(voucherExpiringDays + 1);
            var lowRatingReviewFrom = now.Date.AddDays(-lowRatingReviewDays);

            var pendingOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(o => o.OrderStatusId == (int)OrderStatusEnum.ChoXacNhan);

            var pendingPayments = await _context.Payments
                .AsNoTracking()
                .CountAsync(p =>
                    p.PaymentMethod != "COD" &&
                    (p.Status == null || !PaidPaymentStatuses.Contains(p.Status)));

            var lowStockVariants = await _context.ProductDetails
                .AsNoTracking()
                .CountAsync(d => (d.StockQuantity ?? 0) > 0 && (d.StockQuantity ?? 0) <= lowStockThreshold);

            var outOfStockVariants = await _context.ProductDetails
                .AsNoTracking()
                .CountAsync(d => (d.StockQuantity ?? 0) == 0);

            var defectiveSerials = await _context.ProductSerials
                .AsNoTracking()
                .CountAsync(s => s.Status == ProductSerialStatus.Defective);

            var newLowRatingReviews = await _context.Reviews
                .AsNoTracking()
                .CountAsync(r => r.Rating <= 2 && r.ReviewDate >= lowRatingReviewFrom);

            var hiddenReviews = await _context.Reviews
                .AsNoTracking()
                .CountAsync(r => !r.IsVisible);

            var expiringVouchers = await _context.Vouchers
                .AsNoTracking()
                .CountAsync(v => v.IsActive && v.EndDate.HasValue && v.EndDate.Value >= now.Date && v.EndDate.Value < voucherExpireBefore);

            var almostUsedUpVouchers = await _context.Vouchers
                .AsNoTracking()
                .CountAsync(v => v.IsActive && v.UsageLimit.HasValue && v.UsedCount * 10 >= v.UsageLimit.Value * 9);

            var openServiceTickets = await _context.ServiceTickets
                .AsNoTracking()
                .CountAsync(t => t.Status == null || !ClosedServiceTicketStatuses.Contains(t.Status));

            return new AdminAlertSummaryResponse
            {
                PendingOrders = pendingOrders,
                PendingPayments = pendingPayments,
                LowStockVariants = lowStockVariants,
                OutOfStockVariants = outOfStockVariants,
                DefectiveSerials = defectiveSerials,
                NewLowRatingReviews = newLowRatingReviews,
                HiddenReviews = hiddenReviews,
                ExpiringVouchers = expiringVouchers,
                AlmostUsedUpVouchers = almostUsedUpVouchers,
                OpenServiceTickets = openServiceTickets
            };
        }

        public async Task<(List<SlowMovingProductResponse> Products, int TotalCount)> GetSlowMovingProductsAsync(
            int daysWithoutSale = 30,
            int page = 1,
            int pageSize = 10)
        {
            var now = DateTime.UtcNow;
            var cutoffDate = now.Date.AddDays(-daysWithoutSale);

            var query = _context.Products
                .AsNoTracking()
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.MainImageUrl,
                    CategoryName = p.Category != null ? p.Category.CategoryName : null,
                    BrandName = p.Brand != null ? p.Brand.BrandName : null,
                    TotalStock = p.ProductDetails.Sum(d => d.StockQuantity ?? 0),
                    TotalSold = p.ProductDetails
                        .SelectMany(d => d.OrderDetails)
                        .Where(od =>
                            od.Order != null &&
                            (od.Order.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                             od.Order.OrderStatusId == (int)OrderStatusEnum.HoanTat))
                        .Sum(od => (int?)od.Quantity) ?? 0,
                    LastSoldDate = p.ProductDetails
                        .SelectMany(d => d.OrderDetails)
                        .Where(od =>
                            od.Order != null &&
                            od.Order.OrderDate.HasValue &&
                            (od.Order.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                             od.Order.OrderStatusId == (int)OrderStatusEnum.HoanTat))
                        .Max(od => od.Order!.OrderDate)
                })
                .Where(p => p.TotalStock > 0 && (!p.LastSoldDate.HasValue || p.LastSoldDate.Value < cutoffDate));

            var totalCount = await query.CountAsync();
            var rows = await query
                .OrderBy(p => p.LastSoldDate ?? DateTime.MinValue)
                .ThenByDescending(p => p.TotalStock)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var products = rows.Select(p => new SlowMovingProductResponse
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                MainImageUrl = p.MainImageUrl,
                CategoryName = p.CategoryName,
                BrandName = p.BrandName,
                TotalStock = p.TotalStock,
                TotalSold = p.TotalSold,
                LastSoldDate = p.LastSoldDate,
                DaysWithoutSale = p.LastSoldDate.HasValue
                    ? Math.Max((now.Date - p.LastSoldDate.Value.Date).Days, 0)
                    : daysWithoutSale
            }).ToList();

            return (products, totalCount);
        }

        private static IQueryable<AdminAuditLog> ApplyAuditDateFilter(
            IQueryable<AdminAuditLog> query,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(l => l.CreatedAt >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(l => l.CreatedAt < toExclusive);
            }

            return query;
        }
    }
}
