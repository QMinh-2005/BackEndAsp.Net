using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
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
            var now = DateTime.Now;

            return await _dbset
                // 1. Chỉ lấy mã đang trong thời gian hiệu lực
                .Where(v => v.StartDate <= now && v.EndDate >= now && v.IsActive == true)
                // 2. Chỉ lấy mã hệ thống còn lượt
                .Where(v => v.UsageLimit == null || v.UsedCount < v.UsageLimit)
                // 3. Lọc mã Global HOẶC mã trong ví của User
                .Where(v =>
                    // Nếu là mã Toàn sàn: Kiểm tra số lần User này đã dùng qua bảng OrderVouchers (để tránh lạm dụng mã Global)
                    (v.IsGlobal == true &&
                     _context.OrderVouchers.Count(ov => ov.VoucherId == v.VoucherId && ov.Order.UserId == userId && ov.Order.OrderStatusId != (int)OrderStatusEnum.DaHuy) < v.MaxUsagePerUser)

                    ||

                    // Nếu là mã cá nhân: Phải có trong ví UserVouchers và chưa dùng hết lượt
                    (v.IsGlobal == false &&
                     _context.UserVouchers.Any(uv => uv.UserId == userId && uv.VoucherId == v.VoucherId && uv.CurrentUsageCount < v.MaxUsagePerUser))
                )
                .ToListAsync();
        }
        public async Task<List<Voucher>> GetAllAvailableVouchersAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbset
                .Where(v =>
                           (v.EndDate == null || v.EndDate > now) &&
                           (v.UsageLimit == null || v.UsedCount < v.UsageLimit) && v.IsActive == true)
                .Include(v => v.VoucherPaymentMethods)
                .Include(v => v.VoucherConditions)
                .ToListAsync();
        }
    }
}
