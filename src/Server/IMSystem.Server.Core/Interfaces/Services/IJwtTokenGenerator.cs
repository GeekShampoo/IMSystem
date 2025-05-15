using IMSystem.Server.Domain.Entities; // For User entity
using System.Collections.Generic;

namespace IMSystem.Server.Core.Interfaces.Services
{
    /// <summary>
    /// 定义 JWT Token 生成服务的接口。
    /// </summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// 为指定用户生成 JWT Token。
        /// </summary>
        /// <param name="user">用户信息实体，包含生成 Token 所需的用户信息。</param>
        /// <param name="roles">用户的角色列表（可选），用于在 Token 中声明用户的角色信息。</param>
        /// <returns>一个包含生成的 JWT Token 字符串及其过期时间的元组。</returns>
        (string Token, DateTime ExpiresAt) GenerateToken(User user, IEnumerable<string>? roles = null);
    }
}