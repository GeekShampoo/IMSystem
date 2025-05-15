using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a new group is created.
/// </summary>
public class GroupCreatedEvent : DomainEvent
{
    /// <summary>
    /// The ID of the newly created group.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The name of the newly created group.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// The ID of the user who created the group.
    /// </summary>
    public Guid CreatorUserId { get; }

    public GroupCreatedEvent(Guid groupId, string groupName, Guid creatorUserId)
    {
        GroupId = groupId;
        GroupName = groupName;
        CreatorUserId = creatorUserId;
    }
}