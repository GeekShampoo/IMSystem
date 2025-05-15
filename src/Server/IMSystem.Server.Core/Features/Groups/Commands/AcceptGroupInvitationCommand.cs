using System;
using IMSystem.Protocol.Common;
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// Command to accept a group invitation.
/// </summary>
public class AcceptGroupInvitationCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the group invitation to accept.
    /// </summary>
    public Guid InvitationId { get; }

    /// <summary>
    /// The ID of the user accepting the invitation.
    /// This should typically be derived from the authenticated user's context.
    /// </summary>
    public Guid UserId { get; }

    public AcceptGroupInvitationCommand(Guid invitationId, Guid userId)
    {
        if (invitationId == Guid.Empty)
            throw new ArgumentException("Invitation ID cannot be empty.", nameof(invitationId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        InvitationId = invitationId;
        UserId = userId;
    }
}