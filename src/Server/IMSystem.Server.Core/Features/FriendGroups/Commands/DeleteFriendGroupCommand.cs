using IMSystem.Protocol.Common; // For Result
using MediatR; // For IRequest and Unit
using System;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

/// <summary>
/// 删除好友分组的命令。
/// </summary>
public class DeleteFriendGroupCommand : IRequest<Result>
{
    /// <summary>
    /// 要删除的好友分组的ID。
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// 执行此操作的用户ID (从认证上下文中获取)。
    /// </summary>
    public Guid CurrentUserId { get; }

    public DeleteFriendGroupCommand(Guid groupId, Guid currentUserId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("分组ID不能为空。", nameof(groupId));
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("当前用户ID不能为空。", nameof(currentUserId));

        GroupId = groupId;
        CurrentUserId = currentUserId;
    }
}