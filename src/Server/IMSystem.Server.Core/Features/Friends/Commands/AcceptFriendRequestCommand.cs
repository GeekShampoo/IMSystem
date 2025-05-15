using IMSystem.Protocol.Common; // For Result
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Friends.Commands;

/// <summary>
/// 接受好友请求的命令。
/// </summary>
public class AcceptFriendRequestCommand : IRequest<Result> // 返回 Result 表示操作成功或失败
{
    /// <summary>
    /// 要接受的好友请求的ID (即 FriendshipId)。
    /// </summary>
    public Guid FriendshipId { get; }

    /// <summary>
    /// 执行此操作的用户ID (即当前登录用户，应该是请求的 Addressee)。
    /// </summary>
    public Guid CurrentUserId { get; }

    public AcceptFriendRequestCommand(Guid friendshipId, Guid currentUserId)
    {
        if (friendshipId == Guid.Empty)
            throw new ArgumentException("好友请求ID不能为空。", nameof(friendshipId));
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("当前用户ID不能为空。", nameof(currentUserId));
            
        FriendshipId = friendshipId;
        CurrentUserId = currentUserId;
    }
}