using IMSystem.Protocol.Enums;
using IMSystem.Protocol.Common; // Added for Result<>
// using IMSystem.Server.Application.Common.Models; // Commented out for now
using MediatR;

namespace IMSystem.Server.Core.Features.Messages.Commands;

public class SendEncryptedMessageCommand : IRequest<Result<Guid>>
{
    public string RecipientId { get; set; } = default!;
    public ProtocolChatType ChatType { get; set; }
    public string EncryptedContent { get; set; } = default!;
    public Guid SenderUserId { get; set; } // Set by the handler or service layer
}