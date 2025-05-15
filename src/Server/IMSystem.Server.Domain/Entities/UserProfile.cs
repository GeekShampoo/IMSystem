using System;
using IMSystem.Server.Domain.Common; // For AuditableEntity or a base class if needed
using IMSystem.Server.Domain.Enums;

namespace IMSystem.Server.Domain.Entities;

/// <summary>
/// Represents the profile information for a user.
/// </summary>
public class UserProfile : AuditableEntity // Inherit from AuditableEntity for CreatedAt, LastModifiedAt etc.
{
    /// <summary>
    /// The ID of the user this profile belongs to. This is also the primary key.
    /// </summary>
    public Guid UserId { get; private set; } // This will be the PK and FK

    /// <summary>
    /// Navigation property to the User.
    /// </summary>
    public virtual User User { get; private set; } = null!;

    /// <summary>
    /// User's nickname.
    /// </summary>
    public string? Nickname { get; private set; }

    /// <summary>
    /// URL of the user's avatar.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// User's gender. Could be an enum or string.
    /// For simplicity, using string for now.
    /// e.g., "Male", "Female", "Other", "PreferNotToSay"
    /// </summary>
    public GenderType? Gender { get; private set; }

    /// <summary>
    /// User's date of birth.
    /// </summary>
    public DateTime? DateOfBirth { get; private set; }

    // public string? Region { get; private set; } // Replaced by structured Address

    /// <summary>
    /// User's structured address information.
    /// </summary>
    public ValueObjects.Address? Address { get; private set; }

    /// <summary>
    /// A short biography or signature of the user.
    /// </summary>
    public string? Bio { get; private set; }

    /// <summary>
    /// Indicates whether this user's profile can be found via user search.
    /// Defaults to true.
    /// </summary>
    public bool AllowSearchability { get; private set; }

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    private UserProfile() : base() { }

    /// <summary>
    /// Creates a new user profile.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="nickname">Initial nickname (can be username by default).</param>
    public UserProfile(Guid userId, string? nickname = null) : base()
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }
        UserId = userId;
        Id = userId; // Set the AuditableEntity's Id to UserId for a 1-to-1 mapping
        Nickname = nickname; // Initially, nickname might be the same as username or null

        // CreatedBy and LastModifiedBy should be set to the userId
        CreatedBy = userId;
        LastModifiedBy = userId;
        AllowSearchability = true; // Default to searchable
    }

    /// <summary>
    /// Updates the user's profile information.
    /// </summary>
    public void UpdateDetails(
        string? nickname,
        string? avatarUrl,
        GenderType? gender,
        DateTime? dateOfBirth,
        ValueObjects.Address? address, // Changed from string? region
        string? bio,
        Guid modifierId)
    {
        // Basic validation or length checks can be added here or via FluentValidation at command level
        Nickname = nickname;
        AvatarUrl = avatarUrl;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Address = address; // Set the structured address
        Bio = bio;

        LastModifiedAt = DateTimeOffset.UtcNow;
        LastModifiedBy = modifierId;
    }

    /// <summary>
    /// Sets the searchability of the user's profile.
    /// </summary>
    /// <param name="allowSearch">True if the profile should be searchable, false otherwise.</param>
    /// <param name="modifierId">The ID of the user performing the modification.</param>
    public void SetSearchability(bool allowSearch, Guid modifierId)
    {
        if (AllowSearchability != allowSearch)
        {
            AllowSearchability = allowSearch;
            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = modifierId;
        }
    }
}