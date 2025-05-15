using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.Friends;

/// <summary>
/// Event raised when a user blocks a friend.
/// </summary>
public class FriendBlockedEvent : DomainEvent
{
    public Guid OperatorUserId { get; }
    public Guid BlockedUserId { get; }
    public Guid FriendshipId { get; }

    public FriendBlockedEvent(Guid operatorUserId, Guid blockedUserId, Guid friendshipId)
    {
        OperatorUserId = operatorUserId;
        BlockedUserId = blockedUserId;
        FriendshipId = friendshipId;
    }
}