using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a group's announcement is set or updated.
/// </summary>
public class GroupAnnouncementSetEvent : DomainEvent
{
    public Guid GroupId { get; }
    public string GroupName { get; } // For notification context
    public string? Announcement { get; } // The new announcement, null if cleared
    public Guid ActorUserId { get; }
    public string ActorUsername { get; } // For notification context
    public DateTimeOffset? AnnouncementSetAt { get; }

    public GroupAnnouncementSetEvent(
        Guid groupId,
        string groupName,
        string? announcement,
        Guid actorUserId,
        string actorUsername,
        DateTimeOffset? announcementSetAt)
    {
        GroupId = groupId;
        GroupName = groupName;
        Announcement = announcement;
        ActorUserId = actorUserId;
        ActorUsername = actorUsername;
        AnnouncementSetAt = announcementSetAt;
    }
}