using System;
using System.Threading.Tasks;
using IMSystem.Protocol.DTOs.Requests.Auth;
using IMSystem.Protocol.DTOs.Responses.Auth;

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// 认证服务接口
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 当前登录用户的ID
        /// </summary>
        Guid? CurrentUserId { get; }

        /// <summary>
        /// 获取当前的JWT令牌
        /// </summary>
        string? Token { get; }

        /// <summary>
        /// 令牌是否已过期
        /// </summary>
        bool IsTokenExpired { get; }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="request">登录请求参数</param>
        /// <returns>登录结果</returns>
        Task<LoginResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// 用户注销
        /// </summary>
        void Logout();

        /// <summary>
        /// 保存认证信息
        /// </summary>
        /// <param name="response">登录响应信息</param>
        void SaveAuthentication(LoginResponse response);

        /// <summary>
        /// 检查是否已认证
        /// </summary>
        /// <returns>是否已认证</returns>
        bool IsAuthenticated();

        /// <summary>
        /// 异步初始化认证服务
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        Task InitializeAsync();
    }
}