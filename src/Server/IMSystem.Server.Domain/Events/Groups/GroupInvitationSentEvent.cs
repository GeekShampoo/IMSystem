using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a group invitation is sent.
/// </summary>
public class GroupInvitationSentEvent : DomainEvent
{
    /// <summary>
    /// The ID of the invitation.
    /// </summary>
    public Guid InvitationId { get; }

    /// <summary>
    /// The ID of the group for which the invitation was sent.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The name of the group.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// The ID of the user who sent the invitation.
    /// </summary>
    public Guid InviterUserId { get; }
    
    /// <summary>
    /// The username of the user who sent the invitation.
    /// </summary>
    public string InviterUsername { get; }

    /// <summary>
    /// The ID of the user who was invited.
    /// </summary>
    public Guid InvitedUserId { get; }
    
    /// <summary>
    /// The username of the user who was invited.
    /// </summary>
    public string InvitedUsername { get; }

    /// <summary>
    /// Optional message included with the invitation.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// The expiration date and time of the invitation, if any.
    /// </summary>
    public DateTime? ExpiresAt { get; }

    public GroupInvitationSentEvent(
        Guid invitationId,
        Guid groupId,
        string groupName,
        Guid inviterUserId,
        string inviterUsername,
        Guid invitedUserId,
        string invitedUsername,
        string? message,
        DateTime? expiresAt)
        : base(entityId: invitationId, triggeredBy: inviterUserId) // 邀请ID作为实体ID，邀请者ID作为触发者ID
    {
        InvitationId = invitationId;
        GroupId = groupId;
        GroupName = groupName;
        InviterUserId = inviterUserId;
        InviterUsername = inviterUsername;
        InvitedUserId = invitedUserId;
        InvitedUsername = invitedUsername;
        Message = message;
        ExpiresAt = expiresAt;
    }
}