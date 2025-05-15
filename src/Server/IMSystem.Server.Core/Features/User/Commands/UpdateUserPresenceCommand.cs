using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.User.Commands;

/// <summary>
/// Command to update a user's presence (online status and last seen time).
/// </summary>
public class UpdateUserPresenceCommand : IRequest<Result>
{
    public Guid UserId { get; }
    public bool IsOnline { get; }

    public UpdateUserPresenceCommand(Guid userId, bool isOnline)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }
        UserId = userId;
        IsOnline = isOnline;
    }
}