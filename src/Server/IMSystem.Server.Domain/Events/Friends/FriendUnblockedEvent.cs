using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.Friends;

/// <summary>
/// Event raised when a user unblocks a friend.
/// </summary>
public class FriendUnblockedEvent : DomainEvent
{
    public Guid OperatorUserId { get; }
    public Guid UnblockedUserId { get; }
    public Guid FriendshipId { get; }

    public FriendUnblockedEvent(Guid operatorUserId, Guid unblockedUserId, Guid friendshipId)
        : base(entityId: friendshipId, triggeredBy: operatorUserId)
    {
        OperatorUserId = operatorUserId;
        UnblockedUserId = unblockedUserId;
        FriendshipId = friendshipId;
    }
}