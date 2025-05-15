using IMSystem.Server.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence;

/// <summary>
/// Repository interface for UserProfile entities.
/// </summary>
public interface IUserProfileRepository : IGenericRepository<UserProfile>
{
    /// <summary>
    /// Gets a user profile by user ID.
    /// Since UserId is the PK for UserProfile, this is equivalent to GetByIdAsync(userId).
    /// This method is provided for clarity.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The user profile if found; otherwise, null.</returns>
    Task<UserProfile?> GetByUserIdAsync(Guid userId);

    // Add any UserProfile-specific methods here if needed in the future.
    // For example:
    // Task<IEnumerable<UserProfile>> SearchProfilesAsync(string searchTerm, CancellationToken cancellationToken = default);
}