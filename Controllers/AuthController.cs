using System.Data;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.Models;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly IPermissionService _permissionService;
        private const string SecretKey = "YourSecretKeyForAuthenticationShouldBeLongEnough";
        private const int TokenExpirationMinutes = 480;
        public AuthController(IUserService userService, IAuthService authService, IPermissionService permissionService)
        {
            _userService = userService;
            _authService = authService;
            _permissionService = permissionService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(CustomerRegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Username and Password are required" });
            }
            if (request.Password.Length < 6)
            {
                return BadRequest(new { message = "Password must be at least 6 characters" });
            }
            try
            {
                var user = request.Adapt<User>();
                await _userService.Create(user, request.Password);
                return Ok(new { message = "Registration successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Registration failed: " + ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(DTO.Request.Customer.LoginRequest request)
        {
            var user = await _userService.Authenticate(request.Email, request.Password);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid username or password" });
            }
            var roles = user.Roles?.Select(r => r.RoleName).ToList() ?? new List<string>();
            var permissions = await _permissionService.GetUserPermissionAsync(user.UserId);
            var token = _authService.GenerateToken(
                secretKey: SecretKey, // Ít nhất 32 ký tự
                minuteExpireTime: TokenExpirationMinutes, // Sống trong 60 phút
                userId: user.UserId.ToString(),
                email: user.Email,
                roles: roles,
                permissions: permissions
            );
            return Ok(new
            {
                message = "Login successful",
                token = token
            });
        }
    }
}
