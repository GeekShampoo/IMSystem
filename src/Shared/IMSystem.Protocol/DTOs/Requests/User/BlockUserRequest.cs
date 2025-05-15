namespace IMSystem.Protocol.DTOs.Requests.User;

/// <summary>
/// Request to block a user.
/// </summary>
public class BlockUserRequest
{
    /// <summary>
    /// Gets or sets the ID of the user to block.
    /// </summary>
    public Guid UserIdToBlock { get; set; }
}