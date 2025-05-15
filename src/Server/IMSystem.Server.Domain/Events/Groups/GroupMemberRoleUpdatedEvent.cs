using IMSystem.Server.Domain.Common;
using IMSystem.Server.Domain.Entities; // For GroupMemberRole
using System;
using IMSystem.Server.Domain.Enums;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a group member's role is updated.
/// </summary>
public class GroupMemberRoleUpdatedEvent : DomainEvent
{
    public Guid GroupId { get; }
    public string GroupName { get; } // For notification context
    public Guid MemberUserId { get; } // The user whose role was changed
    public string MemberUsername { get; } // Username of the member
    public GroupMemberRole OldRole { get; }
    public GroupMemberRole NewRole { get; }
    public Guid ActorUserId { get; } // User who initiated the role change (e.g., Owner)
    public string ActorUsername { get; } // Username of the actor

    public GroupMemberRoleUpdatedEvent(
        Guid groupId,
        string groupName,
        Guid memberUserId,
        string memberUsername,
        GroupMemberRole oldRole,
        GroupMemberRole newRole,
        Guid actorUserId,
        string actorUsername)
        : base(entityId: groupId, triggeredBy: actorUserId) // 群组ID作为实体ID，执行角色更新的用户ID作为触发者ID
    {
        GroupId = groupId;
        GroupName = groupName;
        MemberUserId = memberUserId;
        MemberUsername = memberUsername;
        OldRole = oldRole;
        NewRole = newRole;
        ActorUserId = actorUserId;
        ActorUsername = actorUsername;
    }
}