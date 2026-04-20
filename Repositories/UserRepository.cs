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
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbset
                .Include(u => u.Roles) //Lấy cái roles. Nếu chỉ lấy nguyên tên thì bỏ đi cũng được, phục vụ cho việc Token lấy roles
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<(List<User> Users, int TotalCount)> SearchAsync(string keyword)
        {
            var query = _dbset.AsQueryable();
            query = query.Include(u => u.Roles);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(u => u.FullName.Contains(keyword));
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
    }
}
