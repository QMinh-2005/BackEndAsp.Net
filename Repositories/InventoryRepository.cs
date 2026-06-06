using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;

namespace MyOwnLearning.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly WebBadmintonContext _context;

        public InventoryRepository(WebBadmintonContext context)
        {
            _context = context;
        }

        public async Task<List<LowStockVariantResponse>> GetLowStockVariantsAsync(int threshold = 5)
        {
            var variants = await _context.ProductDetails
                .AsNoTracking()
                .Where(d => (d.StockQuantity ?? 0) <= threshold)
                .OrderBy(d => d.StockQuantity ?? 0)
                .ThenBy(d => d.Product.ProductName)
                .Select(d => new
                {
                    d.DetailId,
                    d.ProductId,
                    d.Product.ProductName,
                    ProductImageUrl = d.Product.MainImageUrl,
                    d.WeightClass,
                    d.GripSize,
                    d.BalancePoint,
                    d.Stiffness,
                    d.Price,
                    StockQuantity = d.StockQuantity ?? 0
                })
                .ToListAsync();

            return variants.Select(v => new LowStockVariantResponse
            {
                DetailId = v.DetailId,
                ProductId = v.ProductId,
                ProductName = v.ProductName,
                ProductImageUrl = v.ProductImageUrl,
                VariantInfo = BuildVariantInfo(v.WeightClass, v.GripSize, v.BalancePoint, v.Stiffness),
                Price = v.Price,
                StockQuantity = v.StockQuantity,
                Threshold = threshold
            }).ToList();
        }

        public async Task<List<VariantSerialsResponse>> GetSerialsByStatusAsync(string status, int page, int pageSize)
        {
            var normalizedStatus = ProductSerialStatus.Normalized(status);

            var serialRows = await _context.ProductSerials
                .AsNoTracking()
                .Where(s => s.Status == normalizedStatus)
                .OrderByDescending(s => s.ImportDate)
                .ThenByDescending(s => s.SerialId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.SerialId,
                    s.DetailId,
                    s.SerialNumber,
                    s.Status,
                    s.ImportDate,
                    s.Detail.WeightClass,
                    s.Detail.GripSize,
                    s.Detail.BalancePoint,
                    s.Detail.Stiffness
                })
                .ToListAsync();

            if (!serialRows.Any())
                return new List<VariantSerialsResponse>();

            var detailIds = serialRows.Select(s => s.DetailId).Distinct().ToList();
            var countRows = await _context.ProductSerials
                .AsNoTracking()
                .Where(s => detailIds.Contains(s.DetailId))
                .GroupBy(s => s.DetailId)
                .Select(g => new
                {
                    DetailId = g.Key,
                    TotalCount = g.Count(),
                    InStockCount = g.Count(s => s.Status == ProductSerialStatus.InStock),
                    SoldCount = g.Count(s => s.Status == ProductSerialStatus.Sold),
                    DefectiveCount = g.Count(s => s.Status == ProductSerialStatus.Defective),
                    ReservedCount = g.Count(s => s.Status == ProductSerialStatus.Reserved)
                })
                .ToDictionaryAsync(x => x.DetailId);

            return serialRows
                .GroupBy(s => new
                {
                    s.DetailId,
                    s.WeightClass,
                    s.GripSize,
                    s.BalancePoint,
                    s.Stiffness
                })
                .Select(g =>
                {
                    countRows.TryGetValue(g.Key.DetailId, out var counts);

                    return new VariantSerialsResponse
                    {
                        DetailId = g.Key.DetailId,
                        VariantInfo = BuildVariantInfo(g.Key.WeightClass, g.Key.GripSize, g.Key.BalancePoint, g.Key.Stiffness),
                        TotalCount = counts?.TotalCount ?? 0,
                        InStockCount = counts?.InStockCount ?? 0,
                        SoldCount = counts?.SoldCount ?? 0,
                        DefectiveCount = counts?.DefectiveCount ?? 0,
                        ReservedCount = counts?.ReservedCount ?? 0,
                        Serials = g.Select(s => new SerialNumberDto
                        {
                            SerialId = s.SerialId,
                            SerialNumber = s.SerialNumber,
                            Status = s.Status,
                            ImportDate = s.ImportDate ?? DateTime.UtcNow
                        }).ToList()
                    };
                })
                .ToList();
        }

        public async Task<bool> MarkSerialAsDefectiveAsync(int serialId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var serial = await _context.ProductSerials
                .Include(s => s.Detail)
                .FirstOrDefaultAsync(s => s.SerialId == serialId);

            if (serial == null)
                return false;

            if (serial.Status == ProductSerialStatus.Defective)
                return true;

            if (serial.Status != ProductSerialStatus.InStock)
                return false;

            serial.Status = ProductSerialStatus.Defective;
            serial.Detail.StockQuantity = Math.Max((serial.Detail.StockQuantity ?? 0) - 1, 0);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        public async Task<bool> MarkSerialAsInStockAsync(int serialId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var serial = await _context.ProductSerials
                .Include(s => s.Detail)
                .FirstOrDefaultAsync(s => s.SerialId == serialId);

            if (serial == null)
                return false;

            if (serial.Status == ProductSerialStatus.InStock)
                return true;

            if (serial.Status != ProductSerialStatus.Defective)
                return false;

            serial.Status = ProductSerialStatus.InStock;
            serial.Detail.StockQuantity = (serial.Detail.StockQuantity ?? 0) + 1;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        private static string BuildVariantInfo(params string?[] parts)
        {
            return string.Join(" - ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
}
