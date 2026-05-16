using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Request.Customer;
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

        // =====================================================
        // PRIVATE HELPER
        // =====================================================

        private async Task RevertOrderAsync(Order order)
        {
            foreach (var orderDetail in order.OrderDetails)
            {
                var detail = await _context.ProductDetails
                    .Include(pd => pd.Product)
                    .Include(ps => ps.ProductSerials)
                    .FirstOrDefaultAsync(d => d.DetailId == orderDetail.DetailId);

                if (detail != null)
                {
                    detail.StockQuantity += orderDetail.Quantity;
                    detail.Product.SoldQuantity -= orderDetail.Quantity;

                    var serialsToRevert = await _context.ProductSerials
                        .Where(ps => ps.OrderDetailId == orderDetail.OrderDetailId)
                        .ToListAsync();

                    foreach (var serial in serialsToRevert)
                    {
                        serial.Status = ProductSerialStatus.InStock;
                        serial.OrderDetailId = null;
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        // =====================================================
        // QUERY
        // =====================================================

        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _dbset.Where(o => o.UserId == userId)
                .Include(o => o.Payment)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Detail)
                        .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSerials)
                // ✅ Include OrderVouchers để trả về thông tin voucher đã áp dụng
                .Include(o => o.OrderVouchers)
                    .ThenInclude(ov => ov.Voucher)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            var order = await _dbset
                .Include(o => o.Payment)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Detail)
                        .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSerials)
                .Include(o => o.OrderVouchers)
                    .ThenInclude(ov => ov.Voucher)
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

            return order;
        }

        public async Task<(List<Order> Orders, int TotalCount)> GetAllOrdersWithDetailsAsync(int page, int pageSize)
        {
            var orders = await _dbset
                .Include(o => o.Payment)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Detail)
                        .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSerials)
                .Include(o => o.OrderVouchers)
                    .ThenInclude(ov => ov.Voucher)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await _dbset.CountAsync();
            return (orders, totalCount);
        }

        public async Task<Order> GetOrderByIdAndUserIdAsync(int orderId, int userId)
        {
            var order = await _dbset
                .Include(o => o.Payment)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Detail)
                        .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSerials)
                .Include(o => o.OrderVouchers)
                    .ThenInclude(ov => ov.Voucher)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.");

            return order;
        }

        public async Task<(List<Order> Orders, int TotalCount)> GetOrdersByStatusIdAsync(int statusId, int page, int pageSize)
        {
            if (!Enum.IsDefined(typeof(OrderStatusEnum), statusId))
                throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");

            var query = _dbset.Where(o => o.OrderStatusId == statusId)
                .Include(o => o.Payment)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Detail)
                        .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSerials)
                .Include(o => o.OrderVouchers)
                    .ThenInclude(ov => ov.Voucher);

            var totalCount = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<(List<Order> Orders, int TotalCount)> SearchOrderAdminAsync(
            decimal? minPrice, decimal? maxPrice, DateTime? orderDate, int? statusId, int page, int pageSize)
        {
            var query = _dbset
                .Include(o => o.Payment)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Detail)
                        .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSerials)
                .Include(o => o.OrderVouchers)
                    .ThenInclude(ov => ov.Voucher)
                .AsQueryable();

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

        // =====================================================
        // CREATE ORDER — Tích hợp Voucher
        // =====================================================

        /// <param name="voucherDetails">
        /// Danh sách voucher đã được validate và tính discount từ VoucherService,
        /// được truyền vào để ghi vào bảng OrderVoucher trong cùng 1 transaction.
        /// </param>
        public async Task<Order> CreateOrderAsync(
            int userId,
            CreateOrderRequest request,
            List<AppliedVoucherDetail> voucherDetails)   // ✅ Thêm tham số voucher
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // --- 1. Validate payment method ---
                var validPaymentMethods = new List<string> { "COD", "Bank Transfer", "E-Wallet" };
                if (!validPaymentMethods.Contains(request.PaymentMethod))
                    throw new ArgumentException("Phương thức thanh toán không hợp lệ. Chỉ chấp nhận: COD, Bank Transfer, E-Wallet.");

                // --- 2. Load product details ---
                var detailsIdRequest = request.OrderDetails.Select(od => od.DetailId).ToList();
                var details = await _context.ProductDetails
                    .Include(pd => pd.Product)
                    .Include(ps => ps.ProductSerials)
                    .Where(pd => detailsIdRequest.Contains(pd.DetailId))
                    .ToListAsync();

                // --- 3. Tạo Order ---
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

                // --- 4. Xử lý từng OrderDetail ---
                decimal subTotal = 0;
                foreach (var itemRequest in request.OrderDetails)
                {
                    var detail = details.FirstOrDefault(d => d.DetailId == itemRequest.DetailId);
                    if (detail == null)
                        throw new Exception($"Không tìm thấy sản phẩm (ID: {itemRequest.DetailId}).");

                    if (itemRequest.Quantity > detail.StockQuantity)
                        throw new InvalidOperationException($"Sản phẩm {detail.Product?.ProductName} không đủ hàng trong kho.");

                    detail.StockQuantity -= itemRequest.Quantity;
                    detail.Product.SoldQuantity += itemRequest.Quantity;

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

                    // Gán Serial
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

                // --- 5. Tính phí ship ---
                decimal shippingFee = subTotal > 500000 ? 30000 : 0;

                // --- 6. Tính Voucher discount ---
                // ✅ Tổng discount từ tất cả voucher, không vượt quá SubTotal
                decimal totalDiscount = voucherDetails.Any()
                    ? Math.Min(voucherDetails.Sum(v => v.DiscountValue), subTotal)
                    : 0;

                // --- 7. Gán giá trị tiền vào Order ---
                order.SubTotal = subTotal;
                order.ShippingFee = shippingFee;
                order.TotalDiscount = totalDiscount;
                order.FinalAmount = subTotal + shippingFee - totalDiscount;

                // --- 8. Lưu OrderVoucher cho từng voucher đã dùng ---
                // ✅ Ghi vào bảng OrderVoucher ngay trong transaction này
                foreach (var appliedVoucher in voucherDetails)
                {
                    order.OrderVouchers.Add(new OrderVoucher
                    {
                        VoucherId = appliedVoucher.VoucherId,
                        AppliedDiscount = appliedVoucher.DiscountValue
                    });
                }

                // --- 9. Xóa các CartItem đã đặt hàng ---
                var cartItemsToRemove = await _context.CartItems
                    .Where(ci => ci.Cart.UserId == userId && detailsIdRequest.Contains(ci.DetailId))
                    .ToListAsync();

                if (cartItemsToRemove.Any())
                    _context.CartItems.RemoveRange(cartItemsToRemove);

                // --- 10. Lưu tất cả ---
                await _dbset.AddAsync(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Load lại OrderStatus để trả về đầy đủ
                await _context.Entry(order).Reference(o => o.OrderStatus).LoadAsync();
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Lỗi khi tạo đơn hàng: " + ex.Message);
            }
        }

        // =====================================================
        // UPDATE STATUS — Giữ nguyên, đã có RevertOrderAsync
        // =====================================================

        public async Task<Order> UpdateStatusOrderAsync(int orderId, int newStatusId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _dbset
                    .Include(o => o.Payment)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Detail)
                            .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    throw new Exception("Không tìm thấy đơn hàng.");

                if (!Enum.IsDefined(typeof(OrderStatusEnum), newStatusId))
                    throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");

                if (newStatusId == (int)OrderStatusEnum.DaHuy && order.OrderStatusId != (int)OrderStatusEnum.DaHuy)
                    await RevertOrderAsync(order);

                order.OrderStatusId = newStatusId;
                _dbset.Update(order);
                await _context.SaveChangesAsync();
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
        public async Task<int> CountSuccessfulUsesAsync(int userId, int voucherId)
        {
            return await _context.OrderVouchers
                .Where(ov => ov.VoucherId == voucherId
                             && ov.Order.UserId == userId
                             && ov.Order.OrderStatusId != (int)OrderStatusEnum.DaHuy) // Không tính đơn đã hủy
                .CountAsync();
        }
    }
}