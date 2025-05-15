using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.User;
using MediatR;

namespace IMSystem.Server.Core.Features.User.Queries;

/// <summary>
/// Query to get the list of users blocked by the current user.
/// </summary>
public class GetBlockedUsersQuery : IRequest<Result<IEnumerable<BlockedUserDto>>>
{
    /// <summary>
    /// Gets or sets the ID of the current user.
    /// </summary>
    public Guid CurrentUserId { get; set; }
}