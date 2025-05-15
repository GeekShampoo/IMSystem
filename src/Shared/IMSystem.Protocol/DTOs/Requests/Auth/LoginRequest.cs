using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.Auth
{
    /// <summary>
    /// 用户登录请求的数据传输对象
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名为必填项。")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名的长度必须在 3 到 50 个字符之间。")]
        public string Username { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码为必填项。")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密码的长度必须在 6 到 100 个字符之间。")]
        public string Password { get; set; }
        
        /// <summary>
        /// 是否记住登录状态
        /// </summary>
        public bool RememberMe { get; set; }
    }
}