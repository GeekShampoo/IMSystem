using IMSystem.Protocol.Common;
using MediatR;

namespace IMSystem.Server.Core.Features.User.Commands;

/// <summary>
/// Command to block a user.
/// </summary>
public class BlockUserCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the ID of the user to block.
    /// </summary>
    public Guid UserIdToBlock { get; set; }

    /// <summary>
    /// Gets or sets the ID of the current user (the one initiating the block).
    /// </summary>
    public Guid CurrentUserId { get; set; }
}