using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Protocol.Common; // For Result<T>
using MediatR;
using System; // For Guid

namespace IMSystem.Server.Core.Features.User.Queries;

/// <summary>
/// 获取当前登录用户个人资料的查询。
/// </summary>
public class GetCurrentUserProfileQuery : IRequest<Result<UserDto>>
{
    /// <summary>
    /// 要获取个人资料的用户的ID。
    /// 此ID应由调用方（例如Controller）从认证上下文中解析并设置。
    /// </summary>
    public Guid UserId { get; }

    public GetCurrentUserProfileQuery(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("用户ID不能为空。", nameof(userId));
        }
        UserId = userId;
    }
}