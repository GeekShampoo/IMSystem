using IMSystem.Server.Domain.Entities;
using System.Collections.Generic; // Required for IEnumerable
using System.Threading.Tasks; // Required for Task
using System; // Required for Guid

namespace IMSystem.Server.Core.Interfaces.Persistence;

/// <summary>
/// Defines the repository interface for managing <see cref="GroupInvitation"/> entities.
/// </summary>
public interface IGroupInvitationRepository : IGenericRepository<GroupInvitation>
{
    /// <summary>
    /// Finds an invitation by group ID and invited user ID.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    /// <param name="invitedUserId">The ID of the invited user.</param>
    /// <returns>The group invitation if found; otherwise, null.</returns>
    Task<GroupInvitation?> FindByGroupAndInvitedUserAsync(Guid groupId, Guid invitedUserId);

    /// <summary>
    /// Gets all pending invitations for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A collection of pending group invitations.</returns>
    Task<IEnumerable<GroupInvitation>> GetPendingInvitationsForUserAsync(Guid userId);

    /// <summary>
    /// Gets all invitations sent by a specific user.
    /// </summary>
    /// <param name="inviterId">The ID of the inviter.</param>
    /// <returns>A collection of group invitations sent by the user.</returns>
    Task<IEnumerable<GroupInvitation>> GetInvitationsSentByUserAsync(Guid inviterId);

    /// <summary>
    /// Gets all invitations associated with a specific group.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    /// <returns>A collection of group invitations for the specified group.</returns>
    Task<IEnumerable<GroupInvitation>> GetInvitationsSentByGroupAsync(Guid groupId);
}