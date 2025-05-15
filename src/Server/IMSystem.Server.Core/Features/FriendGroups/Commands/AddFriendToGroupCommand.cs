using IMSystem.Protocol.Common; // For Result
using MediatR; // For IRequest and Unit
using System;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

/// <summary>
/// 将好友添加到指定分组的命令。
/// </summary>
public class AddFriendToGroupCommand : IRequest<Result>
{
    /// <summary>
    /// 执行此操作的用户ID (分组的拥有者)。
    /// </summary>
    public Guid CurrentUserId { get; }

    /// <summary>
    /// 要将好友添加到的分组ID。
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// 要添加到分组的好友关系ID (FriendshipId)。
    /// </summary>
    public Guid FriendshipId { get; }

    public AddFriendToGroupCommand(Guid currentUserId, Guid groupId, Guid friendshipId)
    {
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("当前用户ID不能为空。", nameof(currentUserId));
        if (groupId == Guid.Empty)
            throw new ArgumentException("分组ID不能为空。", nameof(groupId));
        if (friendshipId == Guid.Empty)
            throw new ArgumentException("好友关系ID不能为空。", nameof(friendshipId));

        CurrentUserId = currentUserId;
        GroupId = groupId;
        FriendshipId = friendshipId;
    }
}