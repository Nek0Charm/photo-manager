using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net; // 使用 BCrypt 处理密码

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 1. 验证唯一性
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest(new { message = "用户名已存在" });

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "邮箱已被注册" });

            // 2. 创建用户并哈希密码
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                // 使用 BCrypt 加密，工作因子设为 11 (平衡安全与性能)
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password) 
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "注册成功" });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 1. 查找用户 (支持用户名或邮箱登录)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

            if (user == null)
                return Unauthorized(new { message = "用户名或密码错误" });

            // 2. 验证密码
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "用户名或密码错误" });

            // 3. 设置 Session (服务器端记录状态)
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);

            // 返回用户信息（不含密码）
            return Ok(new UserResponse 
            { 
                Id = user.Id, 
                Username = user.Username, 
                Email = user.Email 
            });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "已退出登录" });
        }

        // GET: api/auth/me
        // 用于前端刷新页面后检查当前登录状态
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized(new { message = "未登录" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return Unauthorized();
            }

            return Ok(new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            });
        }
    }
}