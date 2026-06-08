using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class ReturnRequestRepository : Repository<ReturnRequest>, IReturnRequestRepository
    {
        public ReturnRequestRepository(WebBadmintonContext context) : base(context)
        {
        }

        public async Task<bool> IsOrderOwnedByUserAsync(int orderId, int userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.OrderId == orderId && o.UserId == userId);
        }

        public async Task<bool> IsOrderReturnableAsync(int orderId, int userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .AnyAsync(o =>
                    o.OrderId == orderId &&
                    o.UserId == userId &&
                    (o.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                     o.OrderStatusId == (int)OrderStatusEnum.HoanTat));
        }

        public async Task<Order?> GetReturnableOrderForUserAsync(int orderId, int userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o =>
                    o.OrderId == orderId &&
                    o.UserId == userId &&
                    (o.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                     o.OrderStatusId == (int)OrderStatusEnum.HoanTat));
        }

        public async Task<bool> HasOpenReturnRequestAsync(int orderId, int userId)
        {
            return await _dbset
                .AsNoTracking()
                .AnyAsync(r =>
                    r.OrderId == orderId &&
                    r.UserId == userId &&
                    r.Status != ReturnRequestConstants.StatusRejected &&
                    r.Status != ReturnRequestConstants.StatusRefunded);
        }

        public async Task<bool> HasDeliveryProofsAsync(int orderId)
        {
            return await _context.OrderDeliveryProofs
                .AsNoTracking()
                .AnyAsync(p => p.OrderId == orderId);
        }

        public async Task<bool> CanAddDeliveryProofAsync(int orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .AnyAsync(o =>
                    o.OrderId == orderId &&
                    (o.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                     o.OrderStatusId == (int)OrderStatusEnum.HoanTat));
        }

        public async Task<ReturnRequest?> GetReturnRequestByIdAsync(int returnRequestId)
        {
            return await BaseReturnRequestQuery()
                .FirstOrDefaultAsync(r => r.ReturnRequestId == returnRequestId);
        }

        public async Task<ReturnRequest?> GetReturnRequestByIdForUserAsync(int returnRequestId, int userId)
        {
            return await BaseReturnRequestQuery()
                .FirstOrDefaultAsync(r => r.ReturnRequestId == returnRequestId && r.UserId == userId);
        }

        public async Task<ReturnRequest> CreateReturnRequestAsync(ReturnRequest request, int newOrderStatusId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.UserId == request.UserId);

                if (order == null)
                    throw new KeyNotFoundException("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.");

                if (order.OrderStatusId != (int)OrderStatusEnum.DaGiaoHang &&
                    order.OrderStatusId != (int)OrderStatusEnum.HoanTat)
                    throw new InvalidOperationException("Chỉ có thể gửi yêu cầu trả hàng/hoàn tiền khi đơn đã giao hàng hoặc hoàn tất.");

                request.OriginalOrderStatusId = order.OrderStatusId;
                order.OrderStatusId = newOrderStatusId;

                await _dbset.AddAsync(request);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var created = await GetReturnRequestByIdAsync(request.ReturnRequestId);
            return created ?? request;
        }

        public async Task<(List<ReturnRequest> Requests, int TotalCount)> GetReturnRequestsByUserAsync(int userId, int page, int pageSize)
        {
            var query = BaseReturnRequestQuery()
                .AsNoTracking()
                .Where(r => r.UserId == userId);

            var totalCount = await query.CountAsync();
            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (requests, totalCount);
        }

        public async Task<(List<ReturnRequest> Requests, int TotalCount)> GetReturnRequestsForAdminAsync(
            string? status,
            int? orderId,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            var query = BaseReturnRequestQuery()
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                var trimmedStatus = status.Trim();
                query = query.Where(r => r.Status == trimmedStatus);
            }

            if (orderId.HasValue)
            {
                query = query.Where(r => r.OrderId == orderId.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(r => r.UserId == userId.Value);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(r => r.RequestedAt >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(r => r.RequestedAt < toExclusive);
            }

            var totalCount = await query.CountAsync();
            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (requests, totalCount);
        }

        public async Task<List<OrderDeliveryProof>> GetDeliveryProofsByOrderIdAsync(int orderId)
        {
            return await _context.OrderDeliveryProofs
                .AsNoTracking()
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<OrderDeliveryProof> AddDeliveryProofAsync(OrderDeliveryProof proof)
        {
            var orderExists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.OrderId == proof.OrderId);

            if (!orderExists)
                throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

            var alreadyHasProof = await HasDeliveryProofsAsync(proof.OrderId);
            if (alreadyHasProof)
                throw new InvalidOperationException("Đơn hàng này đã có minh chứng giao hàng thành công, không cần thêm lại.");

            await _context.OrderDeliveryProofs.AddAsync(proof);
            await _context.SaveChangesAsync();
            return proof;
        }

        public async Task<ReturnRequest> UpdateReturnRequestAsync(
            ReturnRequest request,
            int? newOrderStatusId = null,
            bool restoreOriginalOrderStatus = false)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _dbset.Update(request);

                if (newOrderStatusId.HasValue || restoreOriginalOrderStatus)
                {
                    var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == request.OrderId);
                    if (order == null)
                        throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

                    if (restoreOriginalOrderStatus)
                    {
                        order.OrderStatusId = request.OriginalOrderStatusId ?? (int)OrderStatusEnum.HoanTat;
                    }
                    else
                    {
                        order.OrderStatusId = newOrderStatusId.GetValueOrDefault();
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var updated = await GetReturnRequestByIdAsync(request.ReturnRequestId);
            return updated ?? request;
        }

        private IQueryable<ReturnRequest> BaseReturnRequestQuery()
        {
            return _dbset
                .Include(r => r.ReturnRequestImages)
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderDeliveryProofs)
                .AsSplitQuery();
        }
    }
}
