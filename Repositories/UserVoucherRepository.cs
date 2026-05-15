using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class UserVoucherRepository : Repository<UserVoucher>, IUserVoucherRepository
    {
        public UserVoucherRepository(WebBadmintonContext context) : base(context)
        {
        }
        public async Task<UserVoucher> GetUserVoucherAsync(int userId, int vId)
        {
            return await _dbset.FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == vId);
        }
        public async Task<bool> IsVoucherAlreadySavedAsync(int userId, int voucherId)
        {
            return await _dbset
                .AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
        }
    }
}
