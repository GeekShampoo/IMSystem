using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Requests.Messages;

public class SendEncryptedMessageRequest
{
    public string RecipientId { get; set; } = default!;
    public ProtocolChatType ChatType { get; set; }
    public string EncryptedContent { get; set; } = default!; // 或者 byte[]
}