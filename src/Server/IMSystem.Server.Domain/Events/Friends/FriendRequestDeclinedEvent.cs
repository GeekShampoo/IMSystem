using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Friends;

/// <summary>
/// 当好友请求被拒绝时触发的领域事件。
/// </summary>
public class FriendRequestDeclinedEvent : DomainEvent
{
    /// <summary>
    /// 好友关系的ID。
    /// </summary>
    public Guid FriendshipId { get; }

    /// <summary>
    /// 原请求发送者的用户ID。
    /// </summary>
    public Guid RequesterId { get; }

    /// <summary>
    /// 原请求接收者的用户ID (执行拒绝操作的用户)。
    /// </summary>
    public Guid AddresseeId { get; }
    
    /// <summary>
    /// 执行拒绝操作的用户的用户名 (用于通知 Requester)。
    /// </summary>
    public string DeclinerUsername { get; }


    public FriendRequestDeclinedEvent(Guid friendshipId, Guid requesterId, Guid addresseeId, string declinerUsername)
    {
        FriendshipId = friendshipId;
        RequesterId = requesterId; // The one who initially sent the request
        AddresseeId = addresseeId;   // The one who declined the request
        DeclinerUsername = declinerUsername;
    }
}