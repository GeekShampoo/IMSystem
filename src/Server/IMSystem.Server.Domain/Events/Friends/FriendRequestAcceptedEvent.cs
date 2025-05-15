using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Friends;

/// <summary>
/// 当好友请求被接受时触发的领域事件。
/// </summary>
public class FriendRequestAcceptedEvent : DomainEvent
{
    /// <summary>
    /// 好友关系的ID。
    /// </summary>
    public Guid FriendshipId { get; }

    /// <summary>
    /// 原请求发送者的用户ID (现在是好友)。
    /// </summary>
    public Guid RequesterId { get; }

    /// <summary>
    /// 原请求接收者的用户ID (现在是好友，也是执行接受操作的用户)。
    /// </summary>
    public Guid AddresseeId { get; }

    /// <summary>
    /// 执行接受操作的用户的用户名 (用于通知 Requester)。
    /// </summary>
    public string AccepterUsername { get; }

    /// <summary>
    /// 执行接受操作的用户的昵称 (可选, 用于通知 Requester)。
    /// </summary>
    public string? AccepterNickname { get; }

    /// <summary>
    /// 执行接受操作的用户的头像URL (可选, 用于通知 Requester)。
    /// </summary>
    public string? AccepterAvatarUrl { get; }


    public FriendRequestAcceptedEvent(Guid friendshipId, Guid requesterId, Guid addresseeId, string accepterUsername, string? accepterNickname, string? accepterAvatarUrl)
    {
        FriendshipId = friendshipId;
        RequesterId = requesterId; // The one who initially sent the request
        AddresseeId = addresseeId;   // The one who accepted the request
        AccepterUsername = accepterUsername;
        AccepterNickname = accepterNickname;
        AccepterAvatarUrl = accepterAvatarUrl;
    }
}