namespace IMSystem.Protocol.DTOs.Responses.User;

public class UserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? Nickname { get; set; } // From UserProfile
    public string? AvatarUrl { get; set; } // From UserProfile
    public string Email { get; set; } = null!; // Keep Email on UserDto as it's part of User entity
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsOnline { get; set; }
    public string? CustomStatus { get; set; }
    
    // Other profile fields can be added if needed for a "full" UserDto,
    // or a separate UserProfileDto can be created.
    // For search results, Nickname and AvatarUrl are often useful.

    /// <summary>
    /// 获取或设置用户信息是否已与服务器同步（主要用于客户端本地存储）。
    /// </summary>
    public bool IsSynced { get; set; }
}