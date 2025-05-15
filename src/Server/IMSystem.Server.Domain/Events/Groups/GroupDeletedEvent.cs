using System;
using System.Collections.Generic;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a group is deleted.
/// </summary>
public class GroupDeletedEvent : DomainEvent
{
    /// <summary>
    /// The ID of the group that was deleted.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The name of the group that was deleted.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// The ID of the user who deleted the group.
    /// </summary>
    public Guid ActorUserId { get; }

    /// <summary>
    /// The username of the user who deleted the group.
    /// </summary>
    public string ActorUsername { get; }

    /// <summary>
    /// A list of user IDs who were members of the group.
    /// This can be used by handlers to notify former members.
    /// </summary>
    public IEnumerable<Guid> FormerMemberUserIds { get; }

    public GroupDeletedEvent(
        Guid groupId, 
        string groupName, 
        Guid actorUserId, 
        string actorUsername,
        IEnumerable<Guid> formerMemberUserIds)
    {
        GroupId = groupId;
        GroupName = groupName;
        ActorUserId = actorUserId;
        ActorUsername = actorUsername;
        FormerMemberUserIds = formerMemberUserIds ?? new List<Guid>();
    }
}