using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IUserVoucherRepository : IRepository<UserVoucher>
    {
        Task<UserVoucher> GetUserVoucherAsync(int userId, int vId);
        Task<bool> IsVoucherAlreadySavedAsync(int userId, int voucherId);
    }
}
