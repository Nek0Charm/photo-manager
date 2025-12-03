using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [MinLength(6, ErrorMessage = "用户名长度至少为6位")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "密码长度至少为6位")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty; // 允许用户名或邮箱登录

        [Required]
        public string Password { get; set; } = string.Empty;
    }
    
    public class UserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}