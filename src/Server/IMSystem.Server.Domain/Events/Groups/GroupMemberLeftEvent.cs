using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a member leaves a group.
/// </summary>
public class GroupMemberLeftEvent : DomainEvent
{
    public Guid GroupId { get; }
    public string GroupName { get; } // For notification context
    public Guid UserId { get; } // The user who left
    public string Username { get; } // Username of the user who left
    public bool WasKicked { get; } // Differentiates from being kicked
    public Guid? ActorId { get; } // User who initiated the kick, if applicable (null for self-leave)
    public string? ActorUsername { get; }

    public GroupMemberLeftEvent(Guid groupId, string groupName, Guid userId, string username, string? actorUsername, bool wasKicked = false, Guid? actorId = null)
        : base(entityId: groupId, triggeredBy: actorId ?? userId) // 群组ID作为实体ID，操作者ID或离开者ID作为触发者ID
    {
        GroupId = groupId;
        GroupName = groupName;
        UserId = userId;
        Username = username;
        WasKicked = wasKicked; // Will be false for self-leave
        ActorId = actorId;
        ActorUsername = actorUsername;
    }
}