using System;
using System.Collections.Generic;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups; // Assuming GroupInvitationDto will be here
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Queries;

/// <summary>
/// Query to get all pending group invitations for a specific user.
/// </summary>
public class GetPendingGroupInvitationsQuery : IRequest<Result<IEnumerable<GroupInvitationDto>>>
{
    /// <summary>
    /// The ID of the user whose pending invitations are to be retrieved.
    /// </summary>
    public Guid UserId { get; }

    public GetPendingGroupInvitationsQuery(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        UserId = userId;
    }
}