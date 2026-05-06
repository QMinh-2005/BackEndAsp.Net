using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
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
                .Include(u => u.Roles) //Lấy cái roles. Nếu chỉ lấy nguyên tên thì bỏ đi cũng được, phục vụ cho việc Token lấy roles
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<(List<User> Users, int TotalCount)> SearchByNameAsync(string keyword)
        {
            var query = _dbset.AsQueryable();
            query = query.Include(u => u.Roles).Include(u => u.UserProfiles);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // 1. Loại bỏ các khoảng trắng thừa nếu có
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
            if (check != null)
            {
                return true;
            }
            return false;
        }
        public async Task<User> GetUserWithProfileAsync(int userId)
        {
            return await _dbset
                .Include(r => r.Roles)
                .Include(u => u.UserProfiles)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }
    }
}
