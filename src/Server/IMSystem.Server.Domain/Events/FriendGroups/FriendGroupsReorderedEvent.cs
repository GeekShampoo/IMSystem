using IMSystem.Server.Domain.Common;
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Domain.Events.FriendGroups;

/// <summary>
/// Event raised when a user's friend groups have been reordered.
/// </summary>
public class FriendGroupsReorderedEvent : DomainEvent
{
    public Guid UserId { get; }

    /// <summary>
    /// A list of tuples or objects containing GroupId and its new Order.
    /// </summary>
    public List<(Guid GroupId, int NewOrder)> ReorderedGroups { get; }

    public FriendGroupsReorderedEvent(Guid userId, List<(Guid GroupId, int NewOrder)> reorderedGroups)
    {
        UserId = userId;
        ReorderedGroups = reorderedGroups ?? throw new ArgumentNullException(nameof(reorderedGroups));
    }
}