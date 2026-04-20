using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.DTO.Request;
using MyOwnLearning.DTO.Response;
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
        [HttpGet]
        public async Task<IActionResult> GetAll(string keyword)
        {
            var (Users, TotalCount) = await _userService.Search(keyword);
            if (Users == null || TotalCount == 0) { return NotFound(new { Message = "Không tìm thấy người dùng nào." }); }
            var userResponse = Users.Adapt<List<DTO.Response.UserResponse>>();
            return Ok(new
            {
                Total = TotalCount,
                Data = userResponse
            });
        }
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
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber
                    // TUYỆT ĐỐI KHÔNG gán Roles ở đây
                };
                var createdUser = await _userService.CreateUserByAdminAsync(user, request.Password, request.Roles);
                var response = createdUser.Adapt<UserResponse>();
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
    }
}
