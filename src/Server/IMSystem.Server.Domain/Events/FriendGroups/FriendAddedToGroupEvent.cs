using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.FriendGroups;

/// <summary>
/// Event raised when a friend is added to a friend group.
/// </summary>
public class FriendAddedToGroupEvent : DomainEvent
{
    public Guid UserFriendGroupId { get; }
    public Guid UserId { get; }
    public Guid FriendshipId { get; }

    public FriendAddedToGroupEvent(Guid userFriendGroupId, Guid userId, Guid friendshipId)
    {
        UserFriendGroupId = userFriendGroupId;
        UserId = userId;
        FriendshipId = friendshipId;
    }
}