namespace IMSystem.Protocol.DTOs.Responses.User;

/// <summary>
/// Represents a blocked user.
/// </summary>
public class BlockedUserDto
{
    /// <summary>
    /// Gets or sets the ID of the blocked user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the username of the blocked user.
    /// </summary>
    public string? Username { get; set; } // Nullable if username can be missing

    /// <summary>
    /// Gets or sets the date and time when the user was blocked.
    /// </summary>
    public DateTimeOffset BlockedAt { get; set; }
}