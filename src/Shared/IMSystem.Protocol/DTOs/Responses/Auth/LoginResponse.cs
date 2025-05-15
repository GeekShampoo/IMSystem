using System;

namespace IMSystem.Protocol.DTOs.Responses.Auth
{
    /// <summary>
    /// 表示用户登录成功后返回的数据传输对象。
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// 用户的唯一标识符。
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户名。
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 用于身份验证的 JWT Token。
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// JWT Token 的过期时间。
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        /// <summary>
        /// 用户的电子邮件地址（可选）。
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 用户个人资料图片的 URL（可选）。
        /// </summary>
        public string? ProfilePictureUrl { get; set; }
        // public IEnumerable<string> Roles { get; set; } // 如果有角色概念
    }
}