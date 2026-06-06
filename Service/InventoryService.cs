using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Interfaces;

namespace MyOwnLearning.Service
{
    public interface IInventoryService
    {
        Task<List<LowStockVariantResponse>> GetLowStockVariantsAsync(int threshold = 5);
        Task<List<VariantSerialsResponse>> GetSerialsByStatusAsync(string status, int page, int pageSize);
        Task<bool> MarkSerialAsDefectiveAsync(int serialId);
        Task<bool> MarkSerialAsInStockAsync(int serialId);
    }

    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;

        public InventoryService(IInventoryRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<List<LowStockVariantResponse>> GetLowStockVariantsAsync(int threshold = 5)
        {
            if (threshold < 0)
                throw new ArgumentException("Threshold must be greater than or equal to 0.");

            return await _inventoryRepository.GetLowStockVariantsAsync(threshold);
        }

        public async Task<List<VariantSerialsResponse>> GetSerialsByStatusAsync(string status, int page, int pageSize)
        {
            ValidatePaging(page, pageSize);
            return await _inventoryRepository.GetSerialsByStatusAsync(status, page, pageSize);
        }

        public async Task<bool> MarkSerialAsDefectiveAsync(int serialId)
        {
            return await _inventoryRepository.MarkSerialAsDefectiveAsync(serialId);
        }

        public async Task<bool> MarkSerialAsInStockAsync(int serialId)
        {
            return await _inventoryRepository.MarkSerialAsInStockAsync(serialId);
        }

        private static void ValidatePaging(int page, int pageSize)
        {
            if (page <= 0)
                throw new ArgumentException("Page must be greater than 0.");

            if (pageSize <= 0 || pageSize > 100)
                throw new ArgumentException("PageSize must be between 1 and 100.");
        }
    }
}
