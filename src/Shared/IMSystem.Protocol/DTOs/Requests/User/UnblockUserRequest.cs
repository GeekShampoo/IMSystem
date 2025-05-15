namespace IMSystem.Protocol.DTOs.Requests.User;

/// <summary>
/// Request to unblock a user.
/// </summary>
public class UnblockUserRequest
{
    /// <summary>
    /// Gets or sets the ID of the user to unblock.
    /// </summary>
    public Guid UserIdToUnblock { get; set; }
}