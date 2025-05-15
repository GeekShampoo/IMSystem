using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Friends.Commands;

/// <summary>
/// Command to block a friend.
/// </summary>
public class BlockFriendCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the user initiating the block.
    /// </summary>
    public Guid CurrentUserId { get; }

    /// <summary>
    /// The ID of the user to be blocked.
    /// </summary>
    public Guid FriendToBlockUserId { get; }

    public BlockFriendCommand(Guid currentUserId, Guid friendToBlockUserId)
    {
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("Current user ID cannot be empty.", nameof(currentUserId));
        if (friendToBlockUserId == Guid.Empty)
            throw new ArgumentException("Friend to block user ID cannot be empty.", nameof(friendToBlockUserId));
        if (currentUserId == friendToBlockUserId)
            throw new ArgumentException("Cannot block oneself.", nameof(friendToBlockUserId));

        CurrentUserId = currentUserId;
        FriendToBlockUserId = friendToBlockUserId;
    }
}