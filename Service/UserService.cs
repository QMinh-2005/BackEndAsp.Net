using Azure.Core;
using Mapster;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Service
{
    public interface IUserService
    {
        Task<User> Create(User user, string password);
        Task<User?> Authenticate(string email, string password);
        Task<(List<User> users, int TotalCount)> SearchByNameAsync(string keyword);
        Task<User> CreateUserByAdminAsync(User user, string password, IEnumerable<string> roles);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task<bool> UpdateProfileAsync(int userId, ChangeInfoRequest request);
        Task<InfoResponse?> GetInfoAsync(int id);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        public UserService(IUserRepository repository, IAuthService authService)
        {
            _userRepository = repository;
            _authService = authService;
        }
        public async Task<User> Create(User user, string password)
        {
            byte[] salt;
            user.PasswordHash = _authService.HashPassword(password, out salt);
            user.Salt = salt;
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;
            await _userRepository.AddAsync(user);
            return (user);
        }
        public async Task<User?> Authenticate(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return null;
            }
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return null;
            }
            bool isValidPassword = false;
            if (user.Salt != null && user.Salt.Length > 0)
            {
                isValidPassword = _authService.IsValidPassword(password, user.Salt, user.PasswordHash);
            }
            if (!isValidPassword)
            {
                Console.WriteLine($"Password verification failed for user: {email}");
                return null;
            }

            Console.WriteLine($"User authenticated successfully: {email}");

            // authentication successful
            return user;
        }

        public async Task<(List<User> users, int TotalCount)> SearchByNameAsync(string keyword)
        {
            return await _userRepository.SearchByNameAsync(keyword);
        }
        public async Task<User> CreateUserByAdminAsync(User user, string password, IEnumerable<string> roles)
        {
            if (await _userRepository.IsExistEmailAsync(user.Email))
            {
                throw new Exception("Email này đã được sử dụng");
            }

            byte[] salt;
            user.PasswordHash = _authService.HashPassword(password, out salt);
            user.Salt = salt;
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            //var u = await Create(user, password);
            if (roles != null && roles.Any())
            {
                user.Roles = await _userRepository.GetRolesByNamesAsync(roles);
            }
            await _userRepository.AddAsync(user);
            return user;
        }
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            // Lấy user từ Repository (đã có sẵn trong Repository base)
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // Kiểm tra mật khẩu cũ (Sử dụng hàm IsValidPassword của bạn trong AuthService)
            bool isValid = _authService.IsValidPassword(oldPassword, user.Salt, user.PasswordHash);
            if (!isValid) throw new Exception("Mật khẩu cũ không chính xác.");

            // Hash mật khẩu mới và cập nhật
            byte[] salt;
            user.PasswordHash = _authService.HashPassword(newPassword, out salt);
            user.Salt = salt;

            await _userRepository.UpdateAsync(user);
            return true;
        }
        public async Task<bool> UpdateProfileAsync(int userId, ChangeInfoRequest request)
        {

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                user.FullName = request.FullName;
            }
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber;
            }
            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = request.DateOfBirth;
            }
            await _userRepository.UpdateAsync(user);
            return true;

        }
        public async Task<InfoResponse?> GetInfoAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return null;
            }
            var res = user.Adapt<InfoResponse?>();
            return res;
        }
    }
}
