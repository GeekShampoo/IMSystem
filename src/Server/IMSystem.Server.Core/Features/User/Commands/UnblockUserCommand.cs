using IMSystem.Protocol.Common;
using MediatR;

namespace IMSystem.Server.Core.Features.User.Commands;

/// <summary>
/// Command to unblock a user.
/// </summary>
public class UnblockUserCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the ID of the user to unblock.
    /// </summary>
    public Guid UserIdToUnblock { get; set; }

    /// <summary>
    /// Gets or sets the ID of the current user (the one initiating the unblock).
    /// </summary>
    public Guid CurrentUserId { get; set; }
}