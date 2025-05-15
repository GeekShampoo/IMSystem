using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.FriendGroups;

/// <summary>
/// Event raised when a new friend group is created.
/// </summary>
public class FriendGroupCreatedEvent : DomainEvent
{
    public Guid GroupId { get; }
    public Guid UserId { get; } // Owner of the group
    public string Name { get; }
    public int Order { get; }
    public bool IsDefault { get; }

    public FriendGroupCreatedEvent(Guid groupId, Guid userId, string name, int order, bool isDefault)
    {
        GroupId = groupId;
        UserId = userId;
        Name = name;
        Order = order;
        IsDefault = isDefault;
    }
}