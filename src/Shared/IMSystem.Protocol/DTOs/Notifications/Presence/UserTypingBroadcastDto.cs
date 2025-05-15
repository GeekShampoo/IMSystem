using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Notifications;

/// <summary>
/// DTO for broadcasting user typing status to other clients.
/// </summary>
public class UserTypingBroadcastDto
{
    /// <summary>
    /// Gets or sets the chat ID (user ID for private chat, group ID for group chat).
    /// </summary>
    public string ChatId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of chat.
    /// </summary>
    public ProtocolChatType ChatType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who is typing or stopped typing.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the user is typing.
    /// True if the user started typing, false if the user stopped typing.
    /// </summary>
    public bool IsTyping { get; set; }
}