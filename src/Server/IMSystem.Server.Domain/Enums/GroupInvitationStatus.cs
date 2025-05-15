namespace IMSystem.Server.Domain.Enums;

/// <summary>
/// Defines the possible statuses for a group invitation.
/// </summary>
public enum GroupInvitationStatus
{
    /// <summary>
    /// The invitation is pending and awaiting a response.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The invitation has been accepted by the invited user.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// The invitation has been rejected by the invited user.
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// The invitation has been cancelled by the inviter.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// The invitation has expired.
    /// </summary>
    Expired = 4
}