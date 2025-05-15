using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// Command to disband (delete) a group.
/// </summary>
public class DisbandGroupCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the group to be disbanded.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The ID of the user performing the action (should be the group owner).
    /// </summary>
    public Guid ActorUserId { get; }

    public DisbandGroupCommand(Guid groupId, Guid actorUserId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor user ID cannot be empty.", nameof(actorUserId));

        GroupId = groupId;
        ActorUserId = actorUserId;
    }
}