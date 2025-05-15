using IMSystem.Protocol.Common;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations; // For StringLength

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// Command to set or update a group's announcement.
/// </summary>
public class SetGroupAnnouncementCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the group.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The ID of the user performing the action (should be group owner or admin).
    /// </summary>
    public Guid ActorUserId { get; }

    /// <summary>
    /// The new announcement text. Null or empty string will clear the announcement.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Announcement cannot exceed 1000 characters.")] // Matches domain entity validation
    public string? Announcement { get; }

    public SetGroupAnnouncementCommand(Guid groupId, Guid actorUserId, string? announcement)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor user ID cannot be empty.", nameof(actorUserId));

        GroupId = groupId;
        ActorUserId = actorUserId;
        Announcement = announcement;
    }
}