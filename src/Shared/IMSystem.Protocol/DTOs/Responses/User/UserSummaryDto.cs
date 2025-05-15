namespace IMSystem.Protocol.DTOs.Responses.User
{
    public class UserSummaryDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? Nickname { get; set; } // Added for actual nickname
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public string? CustomStatus { get; set; } // For detailed presence status
    }
}