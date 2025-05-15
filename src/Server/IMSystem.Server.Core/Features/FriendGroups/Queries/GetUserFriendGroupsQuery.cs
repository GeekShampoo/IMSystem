using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.FriendGroups;
using MediatR;
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Core.Features.FriendGroups.Queries;

/// <summary>
/// 获取当前用户所有好友分组列表的查询。
/// </summary>
public class GetUserFriendGroupsQuery : IRequest<Result<IEnumerable<FriendGroupDto>>>
{
    /// <summary>
    /// 当前用户的ID (将从认证上下文中获取并设置)。
    /// </summary>
    public Guid CurrentUserId { get; }

    public GetUserFriendGroupsQuery(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new ArgumentException("当前用户ID不能为空。", nameof(currentUserId));
        }
        CurrentUserId = currentUserId;
    }
}