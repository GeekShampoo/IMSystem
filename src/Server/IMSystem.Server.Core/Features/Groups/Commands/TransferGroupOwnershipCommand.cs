using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// Command to transfer ownership of a group.
/// </summary>
public class TransferGroupOwnershipCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the group whose ownership is being transferred.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The ID of the current user performing the action (should be the current owner).
    /// </summary>
    public Guid CurrentOwnerId { get; }

    /// <summary>
    /// The ID of the user to whom ownership will be transferred.
    /// </summary>
    public Guid NewOwnerId { get; }

    public TransferGroupOwnershipCommand(Guid groupId, Guid currentOwnerId, Guid newOwnerId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (currentOwnerId == Guid.Empty)
            throw new ArgumentException("Current owner ID cannot be empty.", nameof(currentOwnerId));
        if (newOwnerId == Guid.Empty)
            throw new ArgumentException("New owner ID cannot be empty.", nameof(newOwnerId));
        if (currentOwnerId == newOwnerId)
            throw new ArgumentException("New owner cannot be the same as the current owner.", nameof(newOwnerId));

        GroupId = groupId;
        CurrentOwnerId = currentOwnerId;
        NewOwnerId = newOwnerId;
    }
}