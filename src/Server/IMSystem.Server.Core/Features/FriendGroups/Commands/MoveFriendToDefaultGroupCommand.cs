using IMSystem.Protocol.Common;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

/// <summary>
/// Command to move a friend to the current user's default friend group.
/// </summary>
public class MoveFriendToDefaultGroupCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the user performing the action (owner of the friend groups).
    /// </summary>
    public Guid CurrentUserId { get; }

    /// <summary>
    /// The ID of the friendship record representing the friend to be moved.
    /// </summary>
    public Guid FriendshipId { get; }

    public MoveFriendToDefaultGroupCommand(Guid currentUserId, Guid friendshipId)
    {
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("Current user ID cannot be empty.", nameof(currentUserId));
        if (friendshipId == Guid.Empty)
            throw new ArgumentException("Friendship ID cannot be empty.", nameof(friendshipId));

        CurrentUserId = currentUserId;
        FriendshipId = friendshipId;
    }
}