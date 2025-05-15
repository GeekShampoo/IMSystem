using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.User.Commands;

/// <summary>
/// Command for a user to deactivate their own account.
/// </summary>
public class DeactivateMyAccountCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the user requesting deactivation (obtained from authenticated context).
    /// </summary>
    public Guid UserId { get; }

    public DeactivateMyAccountCommand(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }
        UserId = userId;
    }
}