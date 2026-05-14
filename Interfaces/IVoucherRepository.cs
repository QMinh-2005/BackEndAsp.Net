using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IVoucherRepository : IRepository<Voucher>
    {
        Task<Voucher?> GetVoucherByCodeAsync(string code);
        Task<Voucher?> GetVoucherByIdAsync(int voucherId);
        Task<List<Voucher>> GetVouchersForDropdownAsync(int userId);
    }
}
