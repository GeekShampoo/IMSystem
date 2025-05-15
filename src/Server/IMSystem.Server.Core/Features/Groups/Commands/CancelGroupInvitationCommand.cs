using System;
using IMSystem.Protocol.Common;
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// Command to cancel a group invitation.
/// </summary>
public class CancelGroupInvitationCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the group invitation to cancel.
    /// </summary>
    public Guid InvitationId { get; }

    /// <summary>
    /// The ID of the user attempting to cancel the invitation.
    /// This user must be the original inviter or have administrative privileges over the group.
    /// </summary>
    public Guid CancellerUserId { get; }

    public CancelGroupInvitationCommand(Guid invitationId, Guid cancellerUserId)
    {
        if (invitationId == Guid.Empty)
            throw new ArgumentException("Invitation ID cannot be empty.", nameof(invitationId));
        if (cancellerUserId == Guid.Empty)
            throw new ArgumentException("Canceller User ID cannot be empty.", nameof(cancellerUserId));

        InvitationId = invitationId;
        CancellerUserId = cancellerUserId;
    }
}