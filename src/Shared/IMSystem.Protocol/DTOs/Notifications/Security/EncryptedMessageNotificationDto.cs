using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Notifications;

public class EncryptedMessageNotificationDto
{
    public Guid MessageId { get; set; }
    public string SenderUserId { get; set; } = default!;
    public string ChatId { get; set; } = default!;
    public ProtocolChatType ChatType { get; set; }
    public string EncryptedContent { get; set; } = default!; // 或者 byte[]
    public DateTimeOffset Timestamp { get; set; }
}