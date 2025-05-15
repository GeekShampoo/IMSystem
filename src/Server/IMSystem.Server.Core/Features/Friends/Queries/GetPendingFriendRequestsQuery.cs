using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Friends;
using MediatR;
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Core.Features.Friends.Queries;

/// <summary>
/// 获取当前用户收到的待处理好友请求列表的查询。
/// </summary>
public class GetPendingFriendRequestsQuery : IRequest<Result<IEnumerable<FriendRequestDto>>>
{
    /// <summary>
    /// 当前用户的ID (将从认证上下文中获取并设置)。
    /// </summary>
    public Guid CurrentUserId { get; }

    public GetPendingFriendRequestsQuery(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new ArgumentException("当前用户ID不能为空。", nameof(currentUserId));
        }
        CurrentUserId = currentUserId;
    }
}