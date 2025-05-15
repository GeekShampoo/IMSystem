using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// Command to promote a group member to an Admin role.
/// </summary>
public class PromoteToAdminCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the group.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The ID of the user performing the action (should be the group owner).
    /// </summary>
    public Guid ActorUserId { get; }

    /// <summary>
    /// The ID of the group member to be promoted to Admin.
    /// </summary>
    public Guid TargetUserId { get; }

    public PromoteToAdminCommand(Guid groupId, Guid actorUserId, Guid targetUserId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor user ID cannot be empty.", nameof(actorUserId));
        if (targetUserId == Guid.Empty)
            throw new ArgumentException("Target user ID cannot be empty.", nameof(targetUserId));
        if (actorUserId == targetUserId)
            throw new ArgumentException("Actor user cannot be the same as the target user for promotion.", nameof(targetUserId));

        GroupId = groupId;
        ActorUserId = actorUserId;
        TargetUserId = targetUserId;
    }
}