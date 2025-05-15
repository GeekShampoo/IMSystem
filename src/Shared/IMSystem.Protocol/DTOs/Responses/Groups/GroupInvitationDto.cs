using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Responses.Groups;

/// <summary>
/// Data Transfer Object for representing a group invitation.
/// </summary>
public class GroupInvitationDto
{
    /// <summary>
    /// Gets or sets the ID of the invitation.
    /// </summary>
    public Guid InvitationId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the group.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the name of the group.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the avatar URL of the group.
    /// </summary>
    public string? GroupAvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who sent the invitation.
    /// </summary>
    public Guid InviterId { get; set; }

    /// <summary>
    /// Gets or sets the username of the user who sent the invitation.
    /// </summary>
    public string InviterUsername { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the nickname of the user who sent the invitation.
    /// </summary>
    public string? InviterNickname { get; set; }

    /// <summary>
    /// Gets or sets the avatar URL of the user who sent the invitation.
    /// </summary>
    public string? InviterAvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who is invited.
    /// </summary>
    public Guid InvitedUserId { get; set; }
    
    /// <summary>
    /// Gets or sets the username of the user who is invited.
    /// </summary>
    public string InvitedUsername { get; set; } = string.Empty; // For completeness, though often the recipient knows their own username

    /// <summary>
    /// Gets or sets the status of the invitation (e.g., "Pending", "Accepted", "Rejected").
    /// </summary>
    public ProtocolGroupInvitationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets an optional message included with the invitation.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the invitation was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration date and time of the invitation.
    /// Can be null if the invitation does not expire.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}