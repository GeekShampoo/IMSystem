using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.FriendGroups;

/// <summary>
/// Event raised when a friend group is deleted.
/// </summary>
public class FriendGroupDeletedEvent : DomainEvent
{
    public Guid GroupId { get; }
    public string Name { get; }
    public Guid UserId { get; }

    public FriendGroupDeletedEvent(Guid groupId, string name, Guid userId)
    {
        GroupId = groupId;
        Name = name;
        UserId = userId;
    }
}