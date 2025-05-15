using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a group invitation is accepted.
/// </summary>
public class GroupInvitationAcceptedEvent : DomainEvent
{
    /// <summary>
    /// The ID of the invitation that was accepted.
    /// </summary>
    public Guid InvitationId { get; }

    /// <summary>
    /// The ID of the group that was joined.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The name of the group that was joined.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// The ID of the user who accepted the invitation and joined the group.
    /// </summary>
    public Guid UserId { get; } // The user who accepted

    /// <summary>
    /// The username of the user who accepted the invitation.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// The ID of the user who originally sent the invitation.
    /// </summary>
    public Guid InviterUserId { get; }


    public GroupInvitationAcceptedEvent(Guid invitationId, Guid groupId, string groupName, Guid userId, string username, Guid inviterUserId)
        : base(entityId: invitationId, triggeredBy: userId) // 邀请ID作为实体ID，接受者ID作为触发者ID
    {
        InvitationId = invitationId;
        GroupId = groupId;
        GroupName = groupName;
        UserId = userId;
        Username = username;
        InviterUserId = inviterUserId;
    }
}