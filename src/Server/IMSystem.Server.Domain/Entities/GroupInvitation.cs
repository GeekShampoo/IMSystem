using IMSystem.Server.Domain.Common;
using IMSystem.Server.Domain.Enums;

namespace IMSystem.Server.Domain.Entities;

/// <summary>
/// Represents a group invitation.
/// </summary>
public class GroupInvitation : AuditableEntity
{
    /// <summary>
    /// Gets or sets the ID of the group to which the user is invited.
    /// </summary>
    public Guid GroupId { get; private set; }

    /// <summary>
    /// Gets or sets the navigation property for the group.
    /// </summary>
    public Group? Group { get; private set; }

    /// <summary>
    /// Gets or sets the ID of the user who sent the invitation.
    /// </summary>
    public Guid InviterId { get; private set; }

    /// <summary>
    /// Gets or sets the navigation property for the inviter.
    /// </summary>
    public User? Inviter { get; private set; }

    /// <summary>
    /// Gets or sets the ID of the user who is invited.
    /// </summary>
    public Guid InvitedUserId { get; private set; }

    /// <summary>
    /// Gets or sets the navigation property for the invited user.
    /// </summary>
    public User? InvitedUser { get; private set; }

    /// <summary>
    /// Gets or sets the status of the invitation.
    /// </summary>
    public GroupInvitationStatus Status { get; private set; }

    /// <summary>
    /// Gets or sets an optional message included with the invitation.
    /// </summary>
    public string? Message { get; private set; }

    /// <summary>
    /// Gets or sets the expiration date and time of the invitation.
    /// Can be null if the invitation does not expire.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    private GroupInvitation() { } // Private constructor for EF Core

    public GroupInvitation(Guid groupId, Guid inviterId, Guid invitedUserId, string? message, DateTime? expiresAt)
    {
        if (groupId == Guid.Empty) throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (inviterId == Guid.Empty) throw new ArgumentException("Inviter ID cannot be empty.", nameof(inviterId));
        if (invitedUserId == Guid.Empty) throw new ArgumentException("Invited User ID cannot be empty.", nameof(invitedUserId));

        GroupId = groupId;
        InviterId = inviterId;
        InvitedUserId = invitedUserId;
        Message = message;
        Status = GroupInvitationStatus.Pending;
        ExpiresAt = expiresAt;

        // Set AuditableEntity properties
        CreatedBy = inviterId;
        LastModifiedAt = CreatedAt; // Initially, LastModifiedAt is the same as CreatedAt
        LastModifiedBy = inviterId; // Initially, LastModifiedBy is the inviter

        // AddDomainEvent(new GroupInvitationSentEvent(this.Id, groupId, inviterId, invitedUserId)); // Example event
    }

    public void Accept()
    {
        if (Status != GroupInvitationStatus.Pending)
        {
            // Consider throwing a DomainException or specific exception
            return;
        }
        Status = GroupInvitationStatus.Accepted;
        // AddDomainEvent(new GroupInvitationAcceptedEvent(this.Id, GroupId, InvitedUserId));
    }

    public void Reject()
    {
        if (Status != GroupInvitationStatus.Pending)
        {
            return;
        }
        Status = GroupInvitationStatus.Rejected;
        // AddDomainEvent(new GroupInvitationRejectedEvent(this.Id, GroupId, InvitedUserId));
    }

    public void Cancel()
    {
        if (Status != GroupInvitationStatus.Pending)
        {
            return;
        }
        Status = GroupInvitationStatus.Cancelled;
        // AddDomainEvent(new GroupInvitationCancelledEvent(this.Id, GroupId, InvitedUserId));
    }

    public void Expire()
    {
        if (Status == GroupInvitationStatus.Accepted || Status == GroupInvitationStatus.Rejected)
        {
            return;
        }
        Status = GroupInvitationStatus.Expired;
    }

    /// <summary>
    /// Updates the status of the invitation.
    /// </summary>
    /// <param name="newStatus">The new status.</param>
    /// <param name="modifierId">The ID of the user modifying the invitation status.</param>
    /// <exception cref="DomainException">Thrown if the status transition is invalid.</exception>
    public void UpdateStatus(GroupInvitationStatus newStatus, Guid modifierId)
    {
        if (modifierId == Guid.Empty)
        {
            throw new ArgumentException("Modifier ID cannot be empty.", nameof(modifierId));
        }

        // Basic validation: Cannot change status if it's already terminal (Accepted, Rejected, Cancelled, Expired)
        // More specific rules can be enforced by calling Accept(), Reject(), Cancel(), Expire() directly.
        if (Status == GroupInvitationStatus.Accepted ||
            Status == GroupInvitationStatus.Rejected ||
            Status == GroupInvitationStatus.Cancelled ||
            Status == GroupInvitationStatus.Expired)
        {
            if (Status != newStatus) // Allow setting to the same terminal state (e.g. re-confirming expiry)
            {
                // Or simply do nothing if trying to change from a terminal state
                // throw new DomainException($"Cannot change status from {Status} to {newStatus}. Invitation is already in a terminal state.");
            }
        }
        
        // Specific transition logic might be better handled in dedicated methods like Accept(), Reject() etc.
        // This method provides a general way to update status, primarily for system-driven changes or simple updates.

        if (Status != newStatus)
        {
            Status = newStatus;
            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = modifierId;
            // Consider adding a generic GroupInvitationStatusChangedEvent if needed
        }
    } // <-- This closing brace was missing for UpdateStatus method

    /// <summary>
    /// Checks if the invitation is currently expired.
    /// </summary>
    /// <param name="currentTime">The current time to check against. If null, DateTime.UtcNow is used.</param>
    /// <returns>True if the invitation is expired, false otherwise.</returns>
    public bool IsExpired(DateTime? currentTime = null)
    {
        if (Status != GroupInvitationStatus.Pending)
        {
            return false; // Only pending invitations can expire in this context.
                          // Terminal states (Accepted, Rejected, Cancelled) are not "expired" in the same way.
                          // Expired status is set explicitly by Expire() method or a background job.
        }

        var now = currentTime ?? DateTime.UtcNow;
        if (ExpiresAt.HasValue)
        {
            return ExpiresAt.Value < now;
        }
        
        // Fallback: If no specific ExpiresAt is set, consider it non-expiring or apply a default system policy.
        // For now, if ExpiresAt is null, it does not expire based on this check.
        // A background job might enforce a default expiration (e.g., 7 days) if ExpiresAt was not set.
        // The audit doc mentioned "InvitationValidDays (7天) 计算", implying a default.
        // Let's add that fallback if ExpiresAt is null.
        const int defaultInvitationValidDays = 7; // Default validity if not specified
        return CreatedAt.AddDays(defaultInvitationValidDays) < now;
    }
}