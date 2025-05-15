using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// Command for a user to leave a group.
/// </summary>
public class LeaveGroupCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the group to leave.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The ID of the user who is leaving the group.
    /// </summary>
    public Guid UserId { get; }

    public LeaveGroupCommand(Guid groupId, Guid userId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        GroupId = groupId;
        UserId = userId;
    }
}