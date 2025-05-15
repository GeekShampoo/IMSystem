using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Friends;

/// <summary>
/// 当好友请求被发送时触发的领域事件。
/// </summary>
public class FriendRequestSentEvent : DomainEvent
{
    /// <summary>
    /// 好友请求的ID。
    /// </summary>
    public Guid FriendshipId { get; }

    /// <summary>
    /// 请求发送者的用户ID。
    /// </summary>
    public Guid RequesterId { get; }

    /// <summary>
    /// 请求接收者的用户ID。
    /// </summary>
    public Guid AddresseeId { get; }

    /// <summary>
    /// 请求发送者的用户名（用于通知）。
    /// </summary>
    public string RequesterUsername { get; }
    
    /// <summary>
    /// 请求发送者的昵称（可选，用于通知）。
    /// </summary>
    public string? RequesterNickname { get; }

    /// <summary>
    /// 请求发送者的头像URL（可选，用于通知）。
    /// </summary>
    public string? RequesterAvatarUrl { get; }


    public FriendRequestSentEvent(Guid friendshipId, Guid requesterId, Guid addresseeId, string requesterUsername, string? requesterNickname, string? requesterAvatarUrl)
    {
        FriendshipId = friendshipId;
        RequesterId = requesterId;
        AddresseeId = addresseeId;
        RequesterUsername = requesterUsername;
        RequesterNickname = requesterNickname;
        RequesterAvatarUrl = requesterAvatarUrl;
    }
}