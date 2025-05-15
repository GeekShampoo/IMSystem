using System;
using System.Collections.Generic;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups;
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Queries;

/// <summary>
/// Query to get all invitations sent by a specific group.
/// </summary>
public class GetSentGroupInvitationsQuery : IRequest<Result<IEnumerable<GroupInvitationDto>>>
{
    /// <summary>
    /// The ID of the group whose sent invitations are to be retrieved.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// The ID of the user requesting the list (for permission checking).
    /// </summary>
    public Guid RequestorUserId { get; }

    public GetSentGroupInvitationsQuery(Guid groupId, Guid requestorUserId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (requestorUserId == Guid.Empty)
            throw new ArgumentException("Requestor User ID cannot be empty.", nameof(requestorUserId));

        GroupId = groupId;
        RequestorUserId = requestorUserId;
    }
}