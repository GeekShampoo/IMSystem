using System;
using IMSystem.Protocol.Common;
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// Command to reject a group invitation.
/// </summary>
public class RejectGroupInvitationCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the group invitation to reject.
    /// </summary>
    public Guid InvitationId { get; }

    /// <summary>
    /// The ID of the user rejecting the invitation.
    /// This should typically be derived from the authenticated user's context.
    /// </summary>
    public Guid UserId { get; }

    public RejectGroupInvitationCommand(Guid invitationId, Guid userId)
    {
        if (invitationId == Guid.Empty)
            throw new ArgumentException("Invitation ID cannot be empty.", nameof(invitationId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        InvitationId = invitationId;
        UserId = userId;
    }
}