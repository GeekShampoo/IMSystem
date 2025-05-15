using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a member is kicked from a group.
/// </summary>
public class GroupMemberKickedEvent : DomainEvent
{
    /// <summary>
    /// The ID of the group from which the member was kicked.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The name of the group.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// The ID of the user who was kicked.
    /// </summary>
    public Guid KickedUserId { get; }

    /// <summary>
    /// The username of the user who was kicked.
    /// </summary>
    public string KickedUsername { get; }

    /// <summary>
    /// The ID of the user who performed the kick action.
    /// </summary>
    public Guid ActorUserId { get; }

    /// <summary>
    /// The username of the user who performed the kick action.
    /// </summary>
    public string ActorUsername { get; }

    public GroupMemberKickedEvent(
        Guid groupId, 
        string groupName,
        Guid kickedUserId, 
        string kickedUsername,
        Guid actorUserId,
        string actorUsername)
        : base(entityId: groupId, triggeredBy: actorUserId) // 群组ID作为实体ID，执行踢出操作的用户ID作为触发者ID
    {
        GroupId = groupId;
        GroupName = groupName;
        KickedUserId = kickedUserId;
        KickedUsername = kickedUsername;
        ActorUserId = actorUserId;
        ActorUsername = actorUsername;
    }
}