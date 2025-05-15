using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when the ownership of a group is transferred.
/// </summary>
public class GroupOwnershipTransferredEvent : DomainEvent
{
    public Guid GroupId { get; }
    public string GroupName { get; } // For notification context
    public Guid OldOwnerUserId { get; }
    public string OldOwnerUsername { get; } // For notification context
    public Guid NewOwnerUserId { get; }
    public string NewOwnerUsername { get; } // For notification context
    public Guid ActorUserId { get; } // User who initiated the transfer (usually the old owner or an admin)

    public GroupOwnershipTransferredEvent(
        Guid groupId, 
        string groupName,
        Guid oldOwnerUserId, 
        string oldOwnerUsername,
        Guid newOwnerUserId,
        string newOwnerUsername,
        Guid actorUserId)
        : base(entityId: groupId, triggeredBy: actorUserId) // 群组ID作为实体ID，执行所有权转移的用户ID作为触发者ID
    {
        GroupId = groupId;
        GroupName = groupName;
        OldOwnerUserId = oldOwnerUserId;
        OldOwnerUsername = oldOwnerUsername;
        NewOwnerUserId = newOwnerUserId;
        NewOwnerUsername = newOwnerUsername;
        ActorUserId = actorUserId;
    }
}