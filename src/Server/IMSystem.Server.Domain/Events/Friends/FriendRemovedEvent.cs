using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Friends;

/// <summary>
/// 当好友关系被移除时触发的领域事件。
/// </summary>
public class FriendRemovedEvent : DomainEvent
{
    /// <summary>
    /// 被移除的好友关系的原ID (可能已不存在于数据库)。
    /// </summary>
    public Guid FriendshipId { get; }

    /// <summary>
    /// 执行移除操作的用户ID。
    /// </summary>
    public Guid RemoverUserId { get; }

    /// <summary>
    /// 被移除的好友的用户ID。
    /// </summary>
    public Guid RemovedFriendUserId { get; }

    /// <summary>
    /// 执行移除操作的用户的用户名 (用于通知被移除者)。
    /// </summary>
    public string RemoverUsername { get; }


    public FriendRemovedEvent(Guid friendshipId, Guid removerUserId, Guid removedFriendUserId, string removerUsername)
    {
        FriendshipId = friendshipId;
        RemoverUserId = removerUserId;
        RemovedFriendUserId = removedFriendUserId;
        RemoverUsername = removerUsername;
    }
}