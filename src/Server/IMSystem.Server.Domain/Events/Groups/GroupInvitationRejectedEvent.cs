using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a group invitation is rejected.
/// </summary>
public class GroupInvitationRejectedEvent : DomainEvent
{
    /// <summary>
    /// The ID of the invitation that was rejected.
    /// </summary>
    public Guid InvitationId { get; }

    /// <summary>
    /// The ID of the group for which the invitation was rejected.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The name of the group.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// The ID of the user who rejected the invitation.
    /// </summary>
    public Guid UserId { get; } // The user who rejected

    /// <summary>
    /// The username of the user who rejected the invitation.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// The ID of the user who originally sent the invitation.
    /// </summary>
    public Guid InviterUserId { get; }

    public GroupInvitationRejectedEvent(Guid invitationId, Guid groupId, string groupName, Guid userId, string username, Guid inviterUserId)
        : base(entityId: invitationId, triggeredBy: userId) // 邀请ID作为实体ID，拒绝者ID作为触发者ID
    {
        InvitationId = invitationId;
        GroupId = groupId;
        GroupName = groupName;
        UserId = userId;
        Username = username;
        InviterUserId = inviterUserId;
    }
}