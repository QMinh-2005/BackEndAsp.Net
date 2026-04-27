using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;

namespace MyOwnLearning.Service
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int UserId, string module, string function);
        Task<List<string>> GetUserPermissionAsync(int UserId);
    }
    public class PermissionService : IPermissionService
    {
        private readonly WebBadmintonContext _context;
        public PermissionService(WebBadmintonContext context)
        {
            _context = context;
        }


        public async Task<bool> HasPermissionAsync(int UserId, string module, string function)
        {

            return await _context.Users
                .Where(u => u.UserId == UserId)
                .SelectMany(u => u.Roles)
                .SelectMany(r => r.RoleModuleFunctions)
                .AnyAsync(rmf => rmf.Module.ModuleName.ToUpper() == module.ToUpper() &&
                rmf.Function.FunctionName.ToUpper() == function.ToUpper());
        }

        public async Task<List<string>> GetUserPermissionAsync(int UserId)
        {
            return await _context.Users
                .Where(u => u.UserId == UserId)
                .SelectMany(u => u.Roles)
                .SelectMany(r => r.RoleModuleFunctions)
                .Select(rmf => $"{rmf.Module.ModuleName.ToUpper()}:{rmf.Function.FunctionName.ToUpper()}")
                .ToListAsync();
        }
    }
}
