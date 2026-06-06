using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using static MyOwnLearning.DTO.Request.Admin.Statistic.StatisticRequest;
using static MyOwnLearning.DTO.Response.Admin.Statistic.StatisticResponse;

namespace MyOwnLearning.Repositories
{
    public class StatisticRepository : IStatisticRepository
    {
        private readonly WebBadmintonContext _context;

        public StatisticRepository(WebBadmintonContext context)
        {
            _context = context;
        }

        private static IQueryable<Order> ApplyOrderDateFilter(IQueryable<Order> query, DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(o => o.OrderDate >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < toExclusive);
            }

            return query;
        }

        private static IQueryable<Order> OnlyRevenueOrders(IQueryable<Order> query)
        {
            return query.Where(o =>
                o.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                o.OrderStatusId == (int)OrderStatusEnum.HoanTat);
        }

        // =====================================================
        // 1. DOANH THU THEO DANH MỤC
        // =====================================================
        public async Task<List<RevenueByCategory>> GetRevenueByCategoryAsync(StatisticsFilterRequest filter)
        {
            var query = _context.OrderDetails
                .Include(od => od.Order)
                    .ThenInclude(o => o.OrderStatus)
                .Include(od => od.Detail)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.Category)
                .Where(od =>
                    // Chỉ tính đơn hàng hoàn tất hoặc đã giao
                    (od.Order.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                     od.Order.OrderStatusId == (int)OrderStatusEnum.HoanTat)
                );

            if (filter.FromDate.HasValue)
                query = query.Where(od => od.Order.OrderDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(od => od.Order.OrderDate <= filter.ToDate.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(od => od.Detail.Product.CategoryId == filter.CategoryId.Value);
            var result = await query
                .GroupBy(od => new
                {
                    CategoryId = od.Detail.Product.CategoryId,
                    CategoryName = od.Detail.Product.Category.CategoryName
                })
                .Select(g => new RevenueByCategory
                {
                    CategoryId = (int)g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    TotalRevenue = g.Sum(od => od.UnitPrice * od.Quantity),
                    TotalOrders = g.Select(od => od.OrderId).Distinct().Count(),
                    TotalItems = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToListAsync();

            return result;
        }

        // =====================================================
        // 2. DOANH THU THEO THƯƠNG HIỆU
        // =====================================================
        public async Task<List<RevenueByBrand>> GetRevenueByBrandAsync(StatisticsFilterRequest filter)
        {
            var query = _context.OrderDetails
                .Include(od => od.Order).ThenInclude(o => o.OrderStatus)
                .Include(od => od.Detail).ThenInclude(d => d.Product).ThenInclude(p => p.Brand)
                .Where(od =>
                    od.Order.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                    od.Order.OrderStatusId == (int)OrderStatusEnum.HoanTat
                );

            if (filter.FromDate.HasValue)
                query = query.Where(od => od.Order.OrderDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(od => od.Order.OrderDate <= filter.ToDate.Value);

            if (filter.BrandId.HasValue)
                query = query.Where(od => od.Detail.Product.BrandId == filter.BrandId.Value);
            var result = await query
                .GroupBy(od => new
                {
                    BrandId = od.Detail.Product.BrandId,
                    BrandName = od.Detail.Product.Brand.BrandName
                })
                .Select(g => new RevenueByBrand
                {
                    BrandId = (int)g.Key.BrandId,
                    BrandName = g.Key.BrandName,
                    TotalRevenue = g.Sum(od => od.UnitPrice * od.Quantity),
                    TotalOrders = g.Select(od => od.OrderId).Distinct().Count(),
                    TotalItems = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToListAsync();

            return result;
        }

        // =====================================================
        // 3. DOANH THU THEO THÁNG / NĂM
        // =====================================================
        public async Task<List<RevenueByMonth>> GetRevenueByMonthAsync(int year)
        {
            var result = await _context.Orders
                .Include(o => o.OrderStatus)
                .Where(o =>
                    o.OrderDate.HasValue &&
                    o.OrderDate.Value.Year == year &&
                    (o.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                     o.OrderStatusId == (int)OrderStatusEnum.HoanTat)
                )
                .GroupBy(o => o.OrderDate.Value.Month)
                .Select(g => new RevenueByMonth
                {
                    Month = g.Key,
                    Year = year,
                    TotalRevenue = g.Sum(o => o.SubTotal ?? 0),
                    TotalOrders = g.Count()
                })
                .OrderBy(r => r.Month)
                .ToListAsync();

            // Đảm bảo đủ 12 tháng, tháng không có doanh thu = 0
            var fullYear = Enumerable.Range(1, 12).Select(m =>
                result.FirstOrDefault(r => r.Month == m) ?? new RevenueByMonth
                {
                    Month = m,
                    Year = year,
                    TotalRevenue = 0,
                    TotalOrders = 0
                }
            ).ToList();

            return fullYear;
        }

        // =====================================================
        // 4. TOP SẢN PHẨM BÁN CHẠY
        // =====================================================
        public async Task<List<TopProductResponse>> GetTopSellingProductsAsync(StatisticsFilterRequest filter, int top = 10)
        {
            var query = _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Detail).ThenInclude(d => d.Product).ThenInclude(p => p.Category)
                .Include(od => od.Detail).ThenInclude(d => d.Product).ThenInclude(p => p.Brand)
                .Where(od =>
                    od.Order.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                    od.Order.OrderStatusId == (int)OrderStatusEnum.HoanTat
                );

            if (filter.FromDate.HasValue)
                query = query.Where(od => od.Order.OrderDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(od => od.Order.OrderDate <= filter.ToDate.Value);
            if (filter.CategoryId.HasValue)
                query = query.Where(od => od.Detail.Product.CategoryId == filter.CategoryId.Value);
            if (filter.BrandId.HasValue)
                query = query.Where(od => od.Detail.Product.BrandId == filter.BrandId.Value);

            return await query
                .GroupBy(od => new
                {
                    od.Detail.Product.ProductId,
                    od.Detail.Product.ProductName,
                    CategoryName = od.Detail.Product.Category.CategoryName,
                    BrandName = od.Detail.Product.Brand.BrandName
                })
                .Select(g => new TopProductResponse
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    BrandName = g.Key.BrandName,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.UnitPrice * od.Quantity)
                })
                .OrderByDescending(r => r.TotalRevenue)
                .Take(top)
                .ToListAsync();
        }

        // =====================================================
        // 5. TỔNG QUAN (KPI CARDS)
        // =====================================================
        public async Task<OverviewStatisticsResponse> GetOverviewAsync(StatisticsFilterRequest filter)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.OrderStatus)
                .Where(o =>
                    o.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                    o.OrderStatusId == (int)OrderStatusEnum.HoanTat
                );

            if (filter.FromDate.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= filter.ToDate.Value);

            var totalRevenue = await ordersQuery.SumAsync(o => o.SubTotal ?? 0);
            var totalOrders = await ordersQuery.CountAsync();
            var cancelledOrders = await _context.Orders.CountAsync(o => o.OrderStatusId == (int)OrderStatusEnum.DaHuy);
            var totalCustomers = await _context.Orders
                .Where(o => filter.FromDate == null || o.OrderDate >= filter.FromDate)
                .Where(o => filter.ToDate == null || o.OrderDate <= filter.ToDate)
                .Select(o => o.UserId)
                .Distinct().CountAsync();

            return new OverviewStatisticsResponse
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                CancelledOrders = cancelledOrders,
                TotalCustomers = totalCustomers,
                AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0
            };
        }

        // =====================================================
        // 6. DOANH THU THEO DANH MỤC + THÁNG (CROSS)
        // =====================================================
        public async Task<List<RevenueCategoryByMonth>> GetRevenueCategoryByMonthAsync(int year, int? categoryId)
        {
            var query = _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Detail).ThenInclude(d => d.Product).ThenInclude(p => p.Category)
                .Where(od =>
                    od.Order.OrderDate.HasValue &&
                    od.Order.OrderDate.Value.Year == year &&
                    (od.Order.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                     od.Order.OrderStatusId == (int)OrderStatusEnum.HoanTat)
                );

            if (categoryId.HasValue)
                query = query.Where(od => od.Detail.Product.CategoryId == categoryId.Value);

            return await query
                .GroupBy(od => new
                {
                    Month = od.Order.OrderDate.Value.Month,
                    CategoryId = od.Detail.Product.CategoryId,
                    CategoryName = od.Detail.Product.Category.CategoryName
                })
                .Select(g => new RevenueCategoryByMonth
                {
                    Month = g.Key.Month,
                    CategoryId = (int)g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    TotalRevenue = g.Sum(od => od.UnitPrice * od.Quantity)
                })
                .OrderBy(r => r.Month)
                .ToListAsync();
        }

        public async Task<List<OrderStatusStatisticResponse>> GetOrderStatusStatisticsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var orderQuery = ApplyOrderDateFilter(_context.Orders.AsNoTracking(), fromDate, toDate);

            var groupedOrders = orderQuery
                .GroupBy(o => o.OrderStatusId)
                .Select(g => new
                {
                    StatusId = g.Key,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o =>
                        o.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                        o.OrderStatusId == (int)OrderStatusEnum.HoanTat
                            ? o.FinalAmount ?? 0
                            : 0)
                });

            var result = await _context.OrderStatuses
                .AsNoTracking()
                .GroupJoin(
                    groupedOrders,
                    status => (int?)status.OrderStatusId,
                    orderGroup => orderGroup.StatusId,
                    (status, orderGroups) => new OrderStatusStatisticResponse
                    {
                        StatusId = status.OrderStatusId,
                        StatusName = status.StatusName,
                        TotalOrders = orderGroups.Select(g => g.TotalOrders).FirstOrDefault(),
                        TotalRevenue = orderGroups.Select(g => g.TotalRevenue).FirstOrDefault()
                    })
                .OrderBy(s => s.StatusId)
                .ToListAsync();

            var totalOrders = result.Sum(s => s.TotalOrders);
            if (totalOrders > 0)
            {
                foreach (var item in result)
                {
                    item.OrderShare = Math.Round(item.TotalOrders * 100.0 / totalOrders, 2);
                }
            }

            return result;
        }

        public async Task<List<PaymentMethodStatisticResponse>> GetRevenueByPaymentMethodAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = OnlyRevenueOrders(ApplyOrderDateFilter(_context.Orders.AsNoTracking(), fromDate, toDate))
                .Where(o => o.Payment != null);

            var result = await query
                .GroupBy(o => o.Payment!.PaymentMethod)
                .Select(g => new PaymentMethodStatisticResponse
                {
                    PaymentMethod = g.Key,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o => o.FinalAmount ?? 0)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            var totalRevenue = result.Sum(x => x.TotalRevenue);
            if (totalRevenue > 0)
            {
                foreach (var item in result)
                {
                    item.RevenueShare = Math.Round((double)(item.TotalRevenue / totalRevenue) * 100, 2);
                }
            }

            return result;
        }

        public async Task<List<VoucherStatisticResponse>> GetVoucherEffectivenessAsync(DateTime? fromDate, DateTime? toDate, int top = 10)
        {
            var orderQuery = OnlyRevenueOrders(ApplyOrderDateFilter(_context.Orders.AsNoTracking(), fromDate, toDate));

            return await _context.OrderVouchers
                .AsNoTracking()
                .Where(ov => orderQuery.Any(o => o.OrderId == ov.OrderId))
                .GroupBy(ov => new
                {
                    ov.VoucherId,
                    ov.Voucher.VoucherCode,
                    ov.Voucher.UsedCount
                })
                .Select(g => new VoucherStatisticResponse
                {
                    VoucherId = g.Key.VoucherId,
                    VoucherCode = g.Key.VoucherCode,
                    CurrentUsedCount = g.Key.UsedCount,
                    TotalOrders = g.Select(ov => ov.OrderId).Distinct().Count(),
                    TotalDiscount = g.Sum(ov => ov.AppliedDiscount),
                    TotalRevenue = g.Sum(ov => ov.Order.FinalAmount ?? 0),
                    AverageDiscountPerOrder = g.Select(ov => ov.OrderId).Distinct().Count() > 0
                        ? g.Sum(ov => ov.AppliedDiscount) / g.Select(ov => ov.OrderId).Distinct().Count()
                        : 0
                })
                .OrderByDescending(x => x.TotalOrders)
                .ThenByDescending(x => x.TotalDiscount)
                .Take(top)
                .ToListAsync();
        }

        public async Task<List<TopCustomerResponse>> GetTopCustomersAsync(DateTime? fromDate, DateTime? toDate, int top = 10)
        {
            var query = OnlyRevenueOrders(ApplyOrderDateFilter(_context.Orders.AsNoTracking(), fromDate, toDate));

            return await query
                .GroupBy(o => new
                {
                    o.UserId,
                    o.User.Email
                })
                .Select(g => new TopCustomerResponse
                {
                    UserId = g.Key.UserId,
                    Email = g.Key.Email,
                    FullName = _context.UserProfiles
                        .Where(p => p.UserId == g.Key.UserId)
                        .OrderBy(p => p.ProfileId)
                        .Select(p => p.FullName)
                        .FirstOrDefault(),
                    PhoneNumber = _context.UserProfiles
                        .Where(p => p.UserId == g.Key.UserId)
                        .OrderBy(p => p.ProfileId)
                        .Select(p => p.PhoneNumber)
                        .FirstOrDefault(),
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.FinalAmount ?? 0),
                    LastOrderDate = g.Max(o => o.OrderDate)
                })
                .OrderByDescending(x => x.TotalSpent)
                .ThenByDescending(x => x.TotalOrders)
                .Take(top)
                .ToListAsync();
        }
    }
}
