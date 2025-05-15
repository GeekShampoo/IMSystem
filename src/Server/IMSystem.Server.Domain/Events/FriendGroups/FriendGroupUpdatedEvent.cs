using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.FriendGroups;

/// <summary>
/// Event raised when an existing friend group's details (name or order) are updated.
/// </summary>
public class FriendGroupUpdatedEvent : DomainEvent
{
    public Guid GroupId { get; }
    public Guid UserId { get; } // Owner of the group
    public string OldName { get; }
    public string NewName { get; }
    public int OldOrder { get; }
    public int NewOrder { get; }
    public bool IsDefault { get; } // To know if it's the default group

    public FriendGroupUpdatedEvent(
        Guid groupId,
        Guid userId,
        string oldName,
        string newName,
        int oldOrder,
        int newOrder,
        bool isDefault)
    {
        GroupId = groupId;
        UserId = userId;
        OldName = oldName;
        NewName = newName;
        OldOrder = oldOrder;
        NewOrder = newOrder;
        IsDefault = isDefault;
    }
}