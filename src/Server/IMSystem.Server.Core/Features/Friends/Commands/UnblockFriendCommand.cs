using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Friends.Commands;

/// <summary>
/// Command to unblock a friend.
/// </summary>
public class UnblockFriendCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the user initiating the unblock.
    /// </summary>
    public Guid CurrentUserId { get; }

    /// <summary>
    /// The ID of the user to be unblocked.
    /// </summary>
    public Guid FriendToUnblockUserId { get; }

    public UnblockFriendCommand(Guid currentUserId, Guid friendToUnblockUserId)
    {
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("Current user ID cannot be empty.", nameof(currentUserId));
        if (friendToUnblockUserId == Guid.Empty)
            throw new ArgumentException("Friend to unblock user ID cannot be empty.", nameof(friendToUnblockUserId));
        // Unblocking oneself is not a concept here, as blocking is between two users.

        CurrentUserId = currentUserId;
        FriendToUnblockUserId = friendToUnblockUserId;
    }
}