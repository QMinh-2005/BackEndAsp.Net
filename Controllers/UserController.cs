using System.Security.Claims;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response.Admin;
using MyOwnLearning.Models;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10)
        {
            var (Users, TotalCount) = await _userService.GetAllUserAsync(page, pageSize);
            if (Users == null || TotalCount == 0) { return NotFound(new { Message = "Không tìm thấy người dùng nào." }); }
            var userResponse = Users.Adapt<List<UserResponse>>();
            return Ok(new
            {
                Total = TotalCount,
                Data = userResponse,
                Page = page,
                PageSize = pageSize
            });
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("search")]
        public async Task<IActionResult> SearchByName(string keyword)
        {
            var (Users, TotalCount) = await _userService.SearchByNameAsync(keyword);
            if (Users == null || TotalCount == 0) { return NotFound(new { Message = "Không tìm thấy người dùng nào." }); }
            var userResponse = Users.Select(u =>
            {
                var res = u.Adapt<UserResponse>();
                var profile = u.UserProfiles.FirstOrDefault();
                if (profile != null)
                {
                    res.FullName = profile.FullName;
                    res.PhoneNumber = profile.PhoneNumber;
                    res.DateOfBirth = profile.DateOfBirth;
                }
                return res;
            }).ToList();
            return Ok(new
            {
                Total = TotalCount,
                Data = userResponse
            });
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateAccount(CreateUserRequest request)
        {
            try
            {
                if (request.Email == null || request.Password == null)
                {
                    return BadRequest(new { message = "Email và Password không được trống" });
                }
                if (request.Password.Length < 6)
                {
                    return BadRequest(new { message = "Password must be at least 6 characters" });
                }
                //Không dùng được adapt vì không thể gán kiểu IEnumerable<string?> với lại 1 object Roles (gồm ID và roleName0 được
                //var user = request.Adapt<User>();  

                // 1. TẠO THỦ CÔNG: Bỏ qua thuộc tính Roles, chỉ lấy thông tin cơ bản
                var user = new User
                {
                    Email = request.Email,
                    UserProfiles = new List<UserProfile>
                    {
                        new UserProfile
                        {
                            FullName = request.FullName,
                            PhoneNumber = request.PhoneNumber,
                            DateOfBirth = request.DateOfBirth
                        }
                    }
                };
                var createdUser = await _userService.CreateUserByAdminAsync(user, request.Password, request.Roles);
                var response = createdUser.Adapt<UserResponse>();
                var profile = createdUser.UserProfiles.FirstOrDefault();
                if (profile != null)
                {
                    response.FullName = profile.FullName;
                    response.PhoneNumber = profile.PhoneNumber;
                    response.DateOfBirth = profile.DateOfBirth;
                }
                return Ok(
                   new
                   {
                       message = "Tạo tài khoản thành công",
                       Data = response
                   });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize] // Bắt buộc phải có Token hợp lệ
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // 1. Lấy User ID từ Claim trong Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Không xác định được người dùng." });

                int userId = int.Parse(userIdClaim);

                // 2. Kiểm tra mật khẩu mới
                if (request.NewPassword.Length < 6)
                    return BadRequest(new { message = "Mật khẩu mới quá ngắn." });

                // 3. Gọi Service xử lý
                var result = await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);

                return Ok(new { message = "Đổi mật khẩu thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize] // Bắt buộc phải đăng nhập
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ChangeInfoRequest request)
        {
            try
            {
                // 1. Lấy User ID từ Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Không xác định được người dùng." });

                int userId = int.Parse(userIdClaim);

                var result = await _userService.UpdateProfileAsync(userId, request);

                if (result)
                {
                    return Ok(new { message = "Cập nhật thông tin thành công." });
                }

                return NotFound(new { message = "Không tìm thấy người dùng." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("user-info")]
        public async Task<IActionResult> GetInfo()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userIdClaim))
                    return Unauthorized(new { message = "Không xác định được người dùng." });
                int userId = int.Parse(userIdClaim);
                var res = await _userService.GetInfoAsync(userId);
                if (res == null) return NotFound(new { message = $"Không tìm thấy {userId}" });
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/{userId:int}/active")]
        public async Task<IActionResult> SetUserActive(int userId, [FromBody] SetActiveRequest request)
        {
            var updated = await _userService.SetUserActiveAsync(userId, request.IsActive);
            if (!updated)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            return Ok(new
            {
                message = request.IsActive ? "Đã kích hoạt tài khoản." : "Đã khóa tài khoản.",
                UserId = userId,
                request.IsActive
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{userId:int}")]
        public async Task<IActionResult> GetUserDetailForAdmin(int userId)
        {
            try
            {
                var user = await _userService.GetUserDetailForAdminAsync(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{userId:int}/orders")]
        public async Task<IActionResult> GetOrdersByUserForAdmin(int userId)
        {
            try
            {
                var orders = await _userService.GetOrdersByUserForAdminAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

    }
}
