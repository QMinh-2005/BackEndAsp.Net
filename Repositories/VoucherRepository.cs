using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class VoucherRepository : Repository<Voucher>, IVoucherRepository
    {
        public VoucherRepository(WebBadmintonContext context) : base(context)
        {
        }

        public async Task<Voucher?> GetVoucherByCodeAsync(string code)
        {
            return await _dbset
                .Include(vc => vc.VoucherConditions)
                .Include(v => v.VoucherPaymentMethods)
                .FirstOrDefaultAsync(v => v.VoucherCode == code);
        }

        public async Task<Voucher?> GetVoucherByIdAsync(int voucherId)
        {
            return await _dbset
                .Include(vc => vc.VoucherConditions)
                .Include(v => v.VoucherPaymentMethods)
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId);
        }

        public async Task<List<Voucher>> GetVouchersForDropdownAsync(int userId)
        {
            var now = DateTime.UtcNow;

            return await _dbset
                .Include(v => v.VoucherPaymentMethods)
                .Include(v => v.VoucherConditions)
                .Where(v => v.StartDate <= now && v.EndDate >= now && v.IsActive == true)
                .Where(v => v.UsageLimit == null || v.UsedCount < v.UsageLimit)
                .Where(v =>
                    (v.IsGlobal == true &&
                     _context.OrderVouchers.Count(ov =>
                         ov.VoucherId == v.VoucherId &&
                         ov.Order.UserId == userId &&
                         ov.Order.OrderStatusId != (int)OrderStatusEnum.DaHuy) < v.MaxUsagePerUser)
                    ||
                    (v.IsGlobal == false &&
                     _context.UserVouchers.Any(uv =>
                         uv.UserId == userId &&
                         uv.VoucherId == v.VoucherId &&
                         uv.CurrentUsageCount < v.MaxUsagePerUser)))
                .ToListAsync();
        }

        public async Task<List<Voucher>> GetAllAvailableVouchersAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbset
                .Where(v =>
                    (v.EndDate == null || v.EndDate > now) &&
                    (v.UsageLimit == null || v.UsedCount < v.UsageLimit) &&
                    v.IsActive == true)
                .Include(v => v.VoucherPaymentMethods)
                .Include(v => v.VoucherConditions)
                .ToListAsync();
        }

        public async Task<(List<VoucherAdminResponse> Vouchers, int TotalCount)> GetVouchersForAdminAsync(
            string? keyword,
            bool? isActive,
            bool? isGlobal,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            var query = _context.Vouchers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmedKeyword = keyword.Trim();
                query = query.Where(v =>
                    EF.Functions.Like(v.VoucherCode, $"%{trimmedKeyword}%") ||
                    (v.Description != null && EF.Functions.Like(v.Description, $"%{trimmedKeyword}%")));
            }

            if (isActive.HasValue)
            {
                query = query.Where(v => v.IsActive == isActive.Value);
            }

            if (isGlobal.HasValue)
            {
                query = query.Where(v => v.IsGlobal == isGlobal.Value);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(v => !v.EndDate.HasValue || v.EndDate.Value >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(v => !v.StartDate.HasValue || v.StartDate.Value < toExclusive);
            }

            var totalCount = await query.CountAsync();

            var vouchers = await query
                .OrderByDescending(v => v.VoucherId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VoucherAdminResponse
                {
                    VoucherId = v.VoucherId,
                    VoucherCode = v.VoucherCode,
                    Description = v.Description,
                    DiscountValue = v.DiscountValue,
                    IsPercent = v.IsPercent,
                    MaxDiscountAmount = v.MaxDiscountAmount,
                    MinOrderValue = v.MinOrderValue,
                    StartDate = v.StartDate,
                    EndDate = v.EndDate,
                    IsGlobal = v.IsGlobal,
                    IsActive = v.IsActive,
                    UsageLimit = v.UsageLimit,
                    UsedCount = v.UsedCount,
                    MaxUsagePerUser = v.MaxUsagePerUser,
                    SavedCount = v.UserVouchers.Count(),
                    ConditionCount = v.VoucherConditions.Count(),
                    AllowedPaymentMethods = v.VoucherPaymentMethods
                        .OrderBy(pm => pm.PaymentMethod)
                        .Select(pm => pm.PaymentMethod)
                        .ToList(),
                    Conditions = v.VoucherConditions
                        .OrderBy(c => c.ConditionId)
                        .Select(c => new VoucherConditionAdminResponse
                        {
                            ConditionId = c.ConditionId,
                            ProductId = c.ProductId,
                            ProductName = c.Product != null ? c.Product.ProductName : null,
                            CategoryId = c.CategoryId,
                            CategoryName = c.Category != null ? c.Category.CategoryName : null,
                            BrandId = c.BrandId,
                            BrandName = c.Brand != null ? c.Brand.BrandName : null
                        })
                        .ToList()
                })
                .AsSplitQuery()
                .ToListAsync();

            return (vouchers, totalCount);
        }

        public async Task<bool> SetVoucherActiveAsync(int voucherId, bool isActive)
        {
            var affectedRows = await _context.Vouchers
                .Where(v => v.VoucherId == voucherId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsActive, isActive));

            return affectedRows > 0;
        }
    }
}
