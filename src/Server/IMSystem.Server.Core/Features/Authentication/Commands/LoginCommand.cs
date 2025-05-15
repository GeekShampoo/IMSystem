using MediatR;
using IMSystem.Protocol.DTOs.Responses.Auth;
using IMSystem.Protocol.Common; // Added for Result

namespace IMSystem.Server.Core.Features.Authentication.Commands
{
    /// <summary>
    /// 表示用户登录的命令。
    /// </summary>
    public class LoginCommand : IRequest<Result<LoginResponse?>> // Changed to Result<LoginResponse?>
    {
        /// <summary>
        /// 获取或设置用户名。
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 获取或设置密码。
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the user initiating the login.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 初始化 <see cref="LoginCommand"/> 类的新实例。
        /// </summary>
        /// <param name="username">用户名。</param>
        /// <param name="password">密码。</param>
        /// <param name="ipAddress">用户的IP地址 (可选)。</param>
        public LoginCommand(string username, string password, string? ipAddress = null)
        {
            Username = username;
            Password = password;
            IpAddress = ipAddress;
        }
    }
}