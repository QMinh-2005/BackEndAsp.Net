using MyOwnLearning.DTO.Response.Admin;

namespace MyOwnLearning.Interfaces
{
    public interface IInventoryRepository
    {
        Task<List<LowStockVariantResponse>> GetLowStockVariantsAsync(int threshold = 5);
        Task<List<VariantSerialsResponse>> GetSerialsByStatusAsync(string status, int page, int pageSize);
        Task<bool> MarkSerialAsDefectiveAsync(int serialId);
        Task<bool> MarkSerialAsInStockAsync(int serialId);
    }
}
