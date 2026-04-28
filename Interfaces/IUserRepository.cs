using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.Models;

namespace MyOwnLearning.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<(List<User> Users, int TotalCount)> GetAllUserAsync(int page, int pageSize);
        Task<User?> GetByEmailAsync(string username);
        Task<(List<User> Users, int TotalCount)> SearchByNameAsync(string keyword);
        Task<List<Role>> GetRolesByNamesAsync(IEnumerable<string> roles);
        Task<bool> IsExistEmailAsync(string email);
    }
}
