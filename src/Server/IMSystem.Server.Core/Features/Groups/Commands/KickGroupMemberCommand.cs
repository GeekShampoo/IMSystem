using System;
using IMSystem.Protocol.Common;
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// Command to kick a member from a group.
/// </summary>
public class KickGroupMemberCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the group from which the member will be kicked.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The User ID of the member to be kicked.
    /// </summary>
    public Guid MemberUserIdToKick { get; }

    /// <summary>
    /// The User ID of the user performing the kick operation (for permission checking).
    /// </summary>
    public Guid ActorUserId { get; }

    public KickGroupMemberCommand(Guid groupId, Guid memberUserIdToKick, Guid actorUserId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (memberUserIdToKick == Guid.Empty)
            throw new ArgumentException("Member User ID to kick cannot be empty.", nameof(memberUserIdToKick));
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor User ID cannot be empty.", nameof(actorUserId));

        GroupId = groupId;
        MemberUserIdToKick = memberUserIdToKick;
        ActorUserId = actorUserId;
    }
}