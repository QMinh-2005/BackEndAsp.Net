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

        public async Task<(List<LowStockVariantResponse> Items, int TotalCount)> GetLowStockVariantsAsync(int threshold = 5)
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
            var totalCount = variants.Count;
            return (variants.Select(v => new LowStockVariantResponse
            {
                DetailId = v.DetailId,
                ProductId = v.ProductId,
                ProductName = v.ProductName,
                ProductImageUrl = v.ProductImageUrl,
                VariantInfo = BuildVariantInfo(v.WeightClass, v.GripSize, v.BalancePoint, v.Stiffness),
                Price = v.Price,
                StockQuantity = v.StockQuantity,
                Threshold = threshold
            }).ToList(), totalCount);
        }

        public async Task<(List<InventorySerialResponse> Items, int TotalCount)> GetSerialsByStatusAsync(string status, int page, int pageSize)
        {
            var normalizedStatus = ProductSerialStatus.Normalized(status);

            var serials = await _context.ProductSerials
        .AsNoTracking()
        .Where(s => s.Status == normalizedStatus)
        .OrderByDescending(s => s.ImportDate)
        .ThenByDescending(s => s.SerialId)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(s => new
        {
            s.SerialId,
            s.SerialNumber,
            s.Status,
            s.ImportDate,
            s.DetailId,
            s.Detail.ProductId,
            s.Detail.Product.ProductName,
            ProductImageUrl = s.Detail.Product.MainImageUrl,
            s.Detail.WeightClass,
            s.Detail.GripSize,
            s.Detail.BalancePoint,
            s.Detail.Stiffness
        })
        .ToListAsync();
            var totalCount = await _context.ProductSerials
                .AsNoTracking()
                .Where(s => s.Status == normalizedStatus)
                .CountAsync();
            var res = serials.Select(s => new InventorySerialResponse
            {
                SerialId = s.SerialId,
                SerialNumber = s.SerialNumber,
                Status = s.Status,
                ImportDate = s.ImportDate,
                DetailId = s.DetailId,
                ProductId = s.ProductId,
                ProductName = s.ProductName,
                ProductImageUrl = s.ProductImageUrl,
                VariantInfo = BuildVariantInfo(s.WeightClass, s.GripSize, s.BalancePoint, s.Stiffness),
            }).ToList();
            return (res, totalCount);
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
