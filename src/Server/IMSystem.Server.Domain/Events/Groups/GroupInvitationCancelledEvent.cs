using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a group invitation is cancelled.
/// </summary>
public class GroupInvitationCancelledEvent : DomainEvent
{
    /// <summary>
    /// The ID of the invitation that was cancelled.
    /// </summary>
    public Guid InvitationId { get; }

    /// <summary>
    /// The ID of the group for which the invitation was cancelled.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The name of the group.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// The ID of the user who was originally invited.
    /// </summary>
    public Guid InvitedUserId { get; }
    
    /// <summary>
    /// The username of the user who was originally invited.
    /// </summary>
    public string InvitedUsername { get; }

    /// <summary>
    /// The ID of the user who cancelled the invitation (either the inviter or an admin/owner).
    /// </summary>
    public Guid CancellerUserId { get; }
    
    /// <summary>
    /// The username of the user who cancelled the invitation.
    /// </summary>
    public string CancellerUsername { get; }


    public GroupInvitationCancelledEvent(
        Guid invitationId, 
        Guid groupId, 
        string groupName,
        Guid invitedUserId, 
        string invitedUsername,
        Guid cancellerUserId,
        string cancellerUsername)
        : base(entityId: invitationId, triggeredBy: cancellerUserId) // 邀请ID作为实体ID，取消邀请的用户ID作为触发者ID
    {
        InvitationId = invitationId;
        GroupId = groupId;
        GroupName = groupName;
        InvitedUserId = invitedUserId;
        InvitedUsername = invitedUsername;
        CancellerUserId = cancellerUserId;
        CancellerUsername = cancellerUsername;
    }
}