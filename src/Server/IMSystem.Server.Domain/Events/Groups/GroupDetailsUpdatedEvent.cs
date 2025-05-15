using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a group's details (name, description, avatar) are updated.
/// </summary>
public class GroupDetailsUpdatedEvent : DomainEvent
{
    /// <summary>
    /// The ID of the group that was updated.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The new name of the group (if changed).
    /// </summary>
    public string? NewName { get; }

    /// <summary>
    /// The old name of the group (if changed).
    /// </summary>
    public string? OldName { get; }
    
    /// <summary>
    /// The ID of the user who performed the update.
    /// </summary>
    public Guid UpdaterUserId { get; }

    public string? NewDescription { get; }
    public string? OldDescription { get; }
    public string? NewAvatarUrl { get; }
    public string? OldAvatarUrl { get; }


    public GroupDetailsUpdatedEvent(
        Guid groupId,
        Guid updaterUserId,
        string? newName,
        string? oldName,
        string? newDescription,
        string? oldDescription,
        string? newAvatarUrl,
        string? oldAvatarUrl)
    {
        GroupId = groupId;
        UpdaterUserId = updaterUserId;
        NewName = newName;
        OldName = oldName;
        NewDescription = newDescription;
        OldDescription = oldDescription;
        NewAvatarUrl = newAvatarUrl;
        OldAvatarUrl = oldAvatarUrl;
    }
}