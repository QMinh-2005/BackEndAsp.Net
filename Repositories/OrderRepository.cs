using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Service;

namespace MyOwnLearning.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(WebBadmintonContext context) : base(context)
        {
        }

        private IQueryable<Order> QueryOrderWithDetails()
        {
            return _dbset
                .Include(o => o.Payment)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Detail)
                        .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSerials)
                .Include(o => o.OrderVouchers)
                    .ThenInclude(ov => ov.Voucher)
                .AsSplitQuery();
        }

        private static IQueryable<OrderSummaryResponse> ProjectOrderSummaries(IQueryable<Order> query)
        {
            return query.Select(o => new OrderSummaryResponse
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                ReceiverName = o.ReceiverName,
                PhoneNumber = o.PhoneNumber,
                FinalAmount = o.FinalAmount,
                StatusId = o.OrderStatusId,
                Status = o.OrderStatus != null ? o.OrderStatus.StatusName : "Chưa xác định",
                PaymentMethod = o.Payment != null ? o.Payment.PaymentMethod : "Chưa xác định",
                FirstProductName = o.OrderDetails
                    .OrderBy(od => od.OrderDetailId)
                    .Select(od => od.Detail.Product.ProductName)
                    .FirstOrDefault() ?? "N/A",
                TotalProducts = o.OrderDetails.Count(),
                CancelReason = o.CancelReason,
                CancelledAt = o.CancelledAt,
                CancelledByUserId = o.CancelledByUserId
            });
        }

        private static IQueryable<OrderResponse> ProjectOrderDetailResponse(IQueryable<Order> query)
        {
            return query.Select(o => new OrderResponse
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                SubTotal = o.SubTotal,
                TotalDiscount = o.TotalDiscount,
                FinalAmount = o.FinalAmount,
                StatusId = o.OrderStatusId,
                ShippingFee = o.ShippingFee,
                Status = o.OrderStatus != null ? o.OrderStatus.StatusName : "Chưa xác định",
                ReceiverName = o.ReceiverName ?? string.Empty,
                PhoneNumber = o.PhoneNumber,
                ShippingAddress = o.ShippingAddress,
                Note = o.Note,
                CancelReason = o.CancelReason,
                CancelledAt = o.CancelledAt,
                CancelledByUserId = o.CancelledByUserId,
                PaymentMethod = o.Payment != null ? o.Payment.PaymentMethod : "Chưa xác định",
                AppliedVouchers = o.OrderVouchers
                    .Select(ov => new AppliedVoucherResponse
                    {
                        VoucherCode = ov.Voucher != null ? ov.Voucher.VoucherCode : string.Empty,
                        AppliedDiscount = ov.AppliedDiscount
                    })
                    .ToList(),
                OrderDetails = o.OrderDetails
                    .OrderBy(od => od.OrderDetailId)
                    .Select(od => new OrderDetailResponse
                    {
                        OrderDetailId = od.OrderDetailId,
                        DetailId = od.DetailId,
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        IsStringingService = od.IsStringingService,
                        StringBrand = od.StringBrand,
                        TensionKg = od.TensionKg,
                        ProductName = od.Detail != null && od.Detail.Product != null
                            ? od.Detail.Product.ProductName
                            : string.Empty,
                        SerialNumbers = od.ProductSerials
                            .Select(ps => ps.SerialNumber)
                            .ToList()
                    })
                    .ToList()
            });
        }

        private async Task RevertOrderResourcesAsync(Order order)
        {
            foreach (var orderDetail in order.OrderDetails)
            {
                var detail = orderDetail.Detail;
                if (detail != null)
                {
                    detail.StockQuantity = (detail.StockQuantity ?? 0) + orderDetail.Quantity;

                    if (detail.Product != null)
                    {
                        detail.Product.SoldQuantity = Math.Max(
                            0,
                            (detail.Product.SoldQuantity ?? 0) - orderDetail.Quantity);
                    }
                }

                foreach (var serial in orderDetail.ProductSerials)
                {
                    serial.Status = ProductSerialStatus.InStock;
                    serial.OrderDetailId = null;
                }
            }

            var voucherIds = order.OrderVouchers
                .Select(ov => ov.VoucherId)
                .Distinct()
                .ToList();

            if (!voucherIds.Any())
                return;

            foreach (var orderVoucher in order.OrderVouchers)
            {
                if (orderVoucher.Voucher != null && orderVoucher.Voucher.UsedCount > 0)
                    orderVoucher.Voucher.UsedCount--;
            }

            var userVouchers = await _context.UserVouchers
                .Where(uv => uv.UserId == order.UserId && voucherIds.Contains(uv.VoucherId))
                .ToListAsync();

            foreach (var userVoucher in userVouchers)
            {
                if (userVoucher.CurrentUsageCount <= 0)
                    continue;

                userVoucher.CurrentUsageCount--;

                if (userVoucher.CurrentUsageCount == 0)
                    userVoucher.UsedDate = null;
            }
        }

        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await QueryOrderWithDetails()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            var order = await QueryOrderWithDetails()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

            return order;
        }

        public async Task<int?> GetOrderStatusIdAsync(int orderId)
        {
            return await _dbset
                .AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => o.OrderStatusId)
                .FirstOrDefaultAsync();
        }

        public async Task<OrderResponse?> GetOrderDetailForAdminAsync(int orderId)
        {
            return await ProjectOrderDetailResponse(
                    _dbset
                        .AsNoTracking()
                        .Where(o => o.OrderId == orderId))
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        public async Task<(List<Order> Orders, int TotalCount)> GetAllOrdersWithDetailsAsync(int page, int pageSize)
        {
            var orders = await QueryOrderWithDetails()
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await _dbset.CountAsync();
            return (orders, totalCount);
        }

        public async Task<(List<OrderSummaryResponse> Orders, int TotalCount)> GetAllOrderSummariesAsync(int page, int pageSize)
        {
            var query = _dbset.AsNoTracking();

            var totalCount = await query.CountAsync();
            var orders = await ProjectOrderSummaries(query)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<Order> GetOrderByIdAndUserIdAsync(int orderId, int userId)
        {
            var order = await QueryOrderWithDetails()
                .Where(o => o.OrderId == orderId && o.UserId == userId)
                .FirstOrDefaultAsync();

            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.");

            return order;
        }

        public async Task<(List<Order> Orders, int TotalCount)> GetOrdersByStatusIdAsync(int statusId, int page, int pageSize)
        {
            if (!Enum.IsDefined(typeof(OrderStatusEnum), statusId))
                throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");

            var query = QueryOrderWithDetails()
                .Where(o => o.OrderStatusId == statusId);

            var totalCount = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<(List<OrderSummaryResponse> Orders, int TotalCount)> GetOrderSummariesByStatusIdAsync(int statusId, int page, int pageSize)
        {
            if (!Enum.IsDefined(typeof(OrderStatusEnum), statusId))
                throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");

            var query = _dbset
                .AsNoTracking()
                .Where(o => o.OrderStatusId == statusId);

            var totalCount = await query.CountAsync();
            var orders = await ProjectOrderSummaries(query)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<(List<Order> Orders, int TotalCount)> SearchOrderAdminAsync(
            decimal? minPrice, decimal? maxPrice, DateTime? orderDate, int? statusId, int page, int pageSize)
        {
            var query = QueryOrderWithDetails().AsQueryable();

            if (minPrice.HasValue)
                query = query.Where(o => o.FinalAmount >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(o => o.FinalAmount <= maxPrice.Value);

            if (statusId.HasValue)
            {
                if (!Enum.IsDefined(typeof(OrderStatusEnum), statusId))
                    throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");

                query = query.Where(o => o.OrderStatusId == statusId.Value);
            }

            if (orderDate.HasValue)
            {
                var startDate = orderDate.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(o => o.OrderDate >= startDate && o.OrderDate < endDate);
            }

            var totalCount = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<(List<OrderSummaryResponse> Orders, int TotalCount)> SearchOrderSummaryAdminAsync(
            decimal? minPrice, decimal? maxPrice, DateTime? orderDate, int? statusId, int page, int pageSize)
        {
            var query = _dbset.AsNoTracking().AsQueryable();

            if (minPrice.HasValue)
                query = query.Where(o => o.FinalAmount >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(o => o.FinalAmount <= maxPrice.Value);

            if (statusId.HasValue)
            {
                if (!Enum.IsDefined(typeof(OrderStatusEnum), statusId))
                    throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");

                query = query.Where(o => o.OrderStatusId == statusId.Value);
            }

            if (orderDate.HasValue)
            {
                var startDate = orderDate.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(o => o.OrderDate >= startDate && o.OrderDate < endDate);
            }

            var totalCount = await query.CountAsync();
            var orders = await ProjectOrderSummaries(query)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        private async Task ApplyVoucherUsageAsync(int userId, List<AppliedVoucherDetail> voucherDetails)
        {
            if (voucherDetails == null || !voucherDetails.Any())
                return;

            var voucherIds = voucherDetails.Select(v => v.VoucherId).Distinct().ToList();
            var vouchers = await _context.Vouchers
                .Where(v => voucherIds.Contains(v.VoucherId))
                .ToDictionaryAsync(v => v.VoucherId);

            var userVouchers = await _context.UserVouchers
                .Where(uv => uv.UserId == userId && voucherIds.Contains(uv.VoucherId))
                .ToDictionaryAsync(uv => uv.VoucherId);

            foreach (var appliedVoucher in voucherDetails)
            {
                if (!vouchers.TryGetValue(appliedVoucher.VoucherId, out var voucher))
                    throw new Exception($"Voucher ID {appliedVoucher.VoucherId} không tồn tại.");

                if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
                    throw new InvalidOperationException($"Mã {voucher.VoucherCode} đã hết lượt sử dụng.");

                voucher.UsedCount++;

                if (voucher.IsGlobal == true)
                {
                    var usedTimes = await _context.OrderVouchers
                        .CountAsync(ov =>
                            ov.VoucherId == voucher.VoucherId &&
                            ov.Order.UserId == userId &&
                            ov.Order.OrderStatusId != (int)OrderStatusEnum.DaHuy);

                    if (usedTimes >= voucher.MaxUsagePerUser)
                        throw new InvalidOperationException($"Bạn đã dùng hết lượt cho phép của mã toàn sàn {voucher.VoucherCode}.");

                    continue;
                }

                if (!userVouchers.TryGetValue(appliedVoucher.VoucherId, out var userVoucher))
                    throw new InvalidOperationException($"Người dùng chưa sở hữu mã {voucher.VoucherCode} trong ví.");

                if (userVoucher.CurrentUsageCount >= voucher.MaxUsagePerUser)
                    throw new InvalidOperationException($"Bạn đã dùng hết lượt cho phép của mã {voucher.VoucherCode}.");

                userVoucher.CurrentUsageCount++;
                userVoucher.UsedDate = DateTime.UtcNow;
            }
        }

        public async Task<Order> CreateOrderAsync(
            int userId,
            CreateOrderRequest request,
            List<AppliedVoucherDetail> voucherDetails)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var validPaymentMethods = new List<string> { "COD", "Bank Transfer", "E-Wallet" };
                if (!validPaymentMethods.Contains(request.PaymentMethod))
                    throw new ArgumentException("Phương thức thanh toán không hợp lệ. Chỉ chấp nhận: COD, Bank Transfer, E-Wallet.");

                var detailsIdRequest = request.OrderDetails.Select(od => od.DetailId).ToList();
                var details = await _context.ProductDetails
                    .Include(pd => pd.Product)
                    .Include(ps => ps.ProductSerials)
                    .Where(pd => detailsIdRequest.Contains(pd.DetailId))
                    .ToListAsync();

                var order = new Order
                {
                    UserId = userId,
                    ShippingAddress = request.ShippingAddress,
                    PhoneNumber = request.PhoneNumber,
                    ReceiverName = request.ReceiverName,
                    OrderDate = DateTime.UtcNow,
                    OrderStatusId = (int)OrderStatusEnum.ChoXacNhan,
                    Payment = new Payment
                    {
                        PaymentMethod = request.PaymentMethod,
                        PaymentDate = DateTime.UtcNow,
                    }
                };

                decimal subTotal = 0;
                foreach (var itemRequest in request.OrderDetails)
                {
                    var detail = details.FirstOrDefault(d => d.DetailId == itemRequest.DetailId);
                    if (detail == null)
                        throw new Exception($"Không tìm thấy sản phẩm (ID: {itemRequest.DetailId}).");

                    if (itemRequest.Quantity > detail.StockQuantity)
                        throw new InvalidOperationException($"Sản phẩm {detail.Product?.ProductName} không đủ hàng trong kho.");

                    detail.StockQuantity -= itemRequest.Quantity;

                    if (detail.Product != null)
                        detail.Product.SoldQuantity = (detail.Product.SoldQuantity ?? 0) + itemRequest.Quantity;

                    decimal currentPrice = detail.Price > 0 ? detail.Price : (detail.Product?.DiscountPrice ?? detail.Product.BasePrice);

                    var orderDetail = new OrderDetail
                    {
                        DetailId = itemRequest.DetailId,
                        Quantity = itemRequest.Quantity,
                        UnitPrice = currentPrice,
                        IsStringingService = itemRequest.IsStringingService,
                        StringBrand = itemRequest.StringBrand,
                        TensionKg = itemRequest.TensionKg
                    };
                    order.OrderDetails.Add(orderDetail);
                    subTotal += currentPrice * itemRequest.Quantity;

                    var serialsToUpdate = detail.ProductSerials
                        .Where(ps => ps.Status == ProductSerialStatus.InStock)
                        .OrderBy(s => s.ImportDate)
                        .Take(itemRequest.Quantity)
                        .ToList();

                    if (serialsToUpdate.Count < itemRequest.Quantity)
                        throw new InvalidOperationException($"Không đủ mã Serial khả dụng cho {detail.Product?.ProductName}.");

                    foreach (var serial in serialsToUpdate)
                    {
                        serial.Status = ProductSerialStatus.Reserved;
                        orderDetail.ProductSerials.Add(serial);
                    }
                }

                decimal shippingFee = subTotal > 500000 ? 30000 : 0;
                decimal totalDiscount = voucherDetails.Any()
                    ? Math.Min(voucherDetails.Sum(v => v.DiscountValue), subTotal)
                    : 0;

                order.SubTotal = subTotal;
                order.ShippingFee = shippingFee;
                order.TotalDiscount = totalDiscount;
                order.FinalAmount = subTotal + shippingFee - totalDiscount;

                foreach (var appliedVoucher in voucherDetails)
                {
                    order.OrderVouchers.Add(new OrderVoucher
                    {
                        VoucherId = appliedVoucher.VoucherId,
                        AppliedDiscount = appliedVoucher.DiscountValue
                    });
                }

                await ApplyVoucherUsageAsync(userId, voucherDetails);

                var cartItemsToRemove = await _context.CartItems
                    .Where(ci => ci.Cart.UserId == userId && detailsIdRequest.Contains(ci.DetailId))
                    .ToListAsync();

                if (cartItemsToRemove.Any())
                    _context.CartItems.RemoveRange(cartItemsToRemove);

                await _dbset.AddAsync(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _context.Entry(order).Reference(o => o.OrderStatus).LoadAsync();
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Lỗi khi tạo đơn hàng: " + ex.Message);
            }
        }

        public async Task<Order> UpdateStatusOrderAsync(int orderId, int newStatusId, OrderDeliveryProof? deliveryProof = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await QueryOrderWithDetails()
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    throw new Exception("Không tìm thấy đơn hàng.");

                if (!Enum.IsDefined(typeof(OrderStatusEnum), newStatusId))
                    throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");

                if (newStatusId == (int)OrderStatusEnum.DaHuy && order.OrderStatusId != (int)OrderStatusEnum.DaHuy)
                    await RevertOrderResourcesAsync(order);

                order.OrderStatusId = newStatusId;

                if (deliveryProof != null)
                {
                    var alreadyHasProof = await _context.OrderDeliveryProofs
                        .AsNoTracking()
                        .AnyAsync(p => p.OrderId == orderId);

                    if (!alreadyHasProof)
                        await _context.OrderDeliveryProofs.AddAsync(deliveryProof);
                }

                await _context.SaveChangesAsync();

                _context.Entry(order).Reference(o => o.OrderStatus).IsLoaded = false;
                await _context.Entry(order).Reference(o => o.OrderStatus).LoadAsync();

                await transaction.CommitAsync();
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Lỗi khi cập nhật trạng thái đơn hàng: " + ex.Message);
            }
        }

        public async Task<Order> CancelOrderAsync(
            int orderId,
            int? userId,
            int cancelledByUserId,
            string reason,
            IReadOnlyCollection<int> allowedStatusIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var query = QueryOrderWithDetails().Where(o => o.OrderId == orderId);

                if (userId.HasValue)
                    query = query.Where(o => o.UserId == userId.Value);

                var order = await query.FirstOrDefaultAsync();

                if (order == null)
                    throw new Exception(userId.HasValue
                        ? "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập."
                        : "Không tìm thấy đơn hàng.");

                if (order.OrderStatusId == (int)OrderStatusEnum.DaHuy)
                    throw new InvalidOperationException("Đơn hàng đã được hủy trước đó.");

                if (!order.OrderStatusId.HasValue || !allowedStatusIds.Contains(order.OrderStatusId.Value))
                {
                    var currentStatus = order.OrderStatusId.HasValue
                        ? ((OrderStatusEnum)order.OrderStatusId.Value).ToString()
                        : "Chưa xác định";

                    throw new InvalidOperationException($"Không thể hủy đơn hàng khi đang ở trạng thái {currentStatus}.");
                }

                await RevertOrderResourcesAsync(order);

                order.OrderStatusId = (int)OrderStatusEnum.DaHuy;
                order.CancelReason = reason.Trim();
                order.CancelledAt = DateTime.UtcNow;
                order.CancelledByUserId = cancelledByUserId;

                await _context.SaveChangesAsync();

                _context.Entry(order).Reference(o => o.OrderStatus).IsLoaded = false;
                await _context.Entry(order).Reference(o => o.OrderStatus).LoadAsync();

                await transaction.CommitAsync();
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Lỗi khi hủy đơn hàng: " + ex.Message);
            }
        }

        public async Task<int> CountSuccessfulUsesAsync(int userId, int voucherId)
        {
            return await _context.OrderVouchers
                .Where(ov => ov.VoucherId == voucherId
                             && ov.Order.UserId == userId
                             && ov.Order.OrderStatusId != (int)OrderStatusEnum.DaHuy)
                .CountAsync();
        }
    }
}
