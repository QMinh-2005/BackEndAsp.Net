using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.DTO.Response;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(WebBadmintonContext context) : base(context) { }

        public async Task<(List<User> Users, int TotalCount)> GetAllUserAsync(int page, int pageSize)
        {
            var query = _dbset.AsQueryable();
            query = query
                .Include(u => u.Roles)
                .Include(u => u.UserProfiles);
            var TotalCount = await query.CountAsync();
            var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (users, TotalCount);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbset
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<(List<User> Users, int TotalCount)> SearchByNameAsync(string keyword)
        {
            var query = _dbset.AsQueryable();
            query = query.Include(u => u.Roles).Include(u => u.UserProfiles);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string cleanedKeyword = keyword.Replace(" ", "");
                string pattern = "%" + string.Join("%", cleanedKeyword.ToCharArray()) + "%";

                query = query.Where(u => u.UserProfiles.Any(p => EF.Functions.Like(p.FullName, pattern)));
            }
            var TotalCount = await query.CountAsync();
            var users = await query.ToListAsync();
            return (users, TotalCount);
        }

        public async Task<List<Role>> GetRolesByNamesAsync(IEnumerable<string> roles)
        {
            return await _context.Roles.Where(r => roles.Contains(r.RoleName)).ToListAsync();
        }

        public async Task<bool> IsExistEmailAsync(string email)
        {
            var check = await _dbset.FirstOrDefaultAsync(u => u.Email == email);
            return check != null;
        }

        public async Task<User> GetUserWithProfileAsync(int userId)
        {
            return await _dbset
                .Include(r => r.Roles)
                .Include(u => u.UserProfiles)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> SetUserActiveAsync(int userId, bool isActive)
        {
            var affectedRows = await _context.Users
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.IsActive, isActive));

            return affectedRows > 0;
        }

        public async Task<UserDetailAdminResponse?> GetUserDetailForAdminAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new UserDetailAdminResponse
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    FullName = u.UserProfiles
                        .OrderBy(p => p.ProfileId)
                        .Select(p => p.FullName)
                        .FirstOrDefault(),
                    PhoneNumber = u.UserProfiles
                        .OrderBy(p => p.ProfileId)
                        .Select(p => p.PhoneNumber)
                        .FirstOrDefault(),
                    DateOfBirth = u.UserProfiles
                        .OrderBy(p => p.ProfileId)
                        .Select(p => p.DateOfBirth)
                        .FirstOrDefault(),
                    City = u.UserProfiles
                        .OrderBy(p => p.ProfileId)
                        .Select(p => p.City)
                        .FirstOrDefault(),
                    District = u.UserProfiles
                        .OrderBy(p => p.ProfileId)
                        .Select(p => p.District)
                        .FirstOrDefault(),
                    DetailedAddress = u.UserProfiles
                        .OrderBy(p => p.ProfileId)
                        .Select(p => p.DetailedAddress)
                        .FirstOrDefault(),
                    Roles = u.Roles
                        .OrderBy(r => r.RoleName)
                        .Select(r => r.RoleName)
                        .ToList(),
                    TotalOrders = u.Orders.Count(),
                    CompletedOrders = u.Orders.Count(o =>
                        o.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                        o.OrderStatusId == (int)OrderStatusEnum.HoanTat),
                    CancelledOrders = u.Orders.Count(o => o.OrderStatusId == (int)OrderStatusEnum.DaHuy),
                    TotalSpent = u.Orders
                        .Where(o =>
                            o.OrderStatusId == (int)OrderStatusEnum.DaGiaoHang ||
                            o.OrderStatusId == (int)OrderStatusEnum.HoanTat)
                        .Sum(o => o.FinalAmount ?? 0),
                    LastOrderDate = u.Orders
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => o.OrderDate)
                        .FirstOrDefault()
                })
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        public async Task<List<OrderResponse>?> GetOrdersByUserForAdminAsync(int userId)
        {
            var userExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserId == userId);

            if (!userExists)
            {
                return null;
            }

            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderResponse
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    SubTotal = o.SubTotal,
                    TotalDiscount = o.TotalDiscount,
                    FinalAmount = o.FinalAmount,
                    Status = o.OrderStatus != null ? o.OrderStatus.StatusName : "Chưa xác định",
                    ShippingFee = o.ShippingFee,
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
                })
                .AsSplitQuery()
                .ToListAsync();
        }
    }
}
