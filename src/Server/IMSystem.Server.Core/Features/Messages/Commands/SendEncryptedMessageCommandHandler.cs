using IMSystem.Protocol.Common; // Added for Result<>
// using IMSystem.Server.Application.Common.Models; // Commented out for now
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Enums;
using IMSystem.Server.Domain.Events;
using IMSystem.Server.Domain.Events.Messages;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Messages.Commands;

public class SendEncryptedMessageCommandHandler : IRequestHandler<SendEncryptedMessageCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendEncryptedMessageCommandHandler> _logger;

    public SendEncryptedMessageCommandHandler(IUnitOfWork unitOfWork, ILogger<SendEncryptedMessageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SendEncryptedMessageCommand request, CancellationToken cancellationToken)
    {
        var sender = await _unitOfWork.Users.GetByIdAsync(request.SenderUserId);
        if (sender == null)
        {
            _logger.LogWarning("Sender not found: {SenderUserId}", request.SenderUserId);
            return Result<Guid>.Failure(new Error("Message.Send.SenderNotFound", "Sender not found."));
        }

        Message message;
        // MessageRecipientType recipientType; // recipientType is assigned but its value is never used

        if (request.ChatType == Protocol.Enums.ProtocolChatType.Private) // Changed from Single to Private
        {
            var recipientUser = await _unitOfWork.Users.GetByIdAsync(Guid.Parse(request.RecipientId), cancellationToken);
            if (recipientUser == null)
            {
                _logger.LogWarning("Recipient user not found: {RecipientId}", request.RecipientId);
                return Result<Guid>.Failure(new Error("Message.Send.RecipientUserNotFound", "Recipient user not found."));
            }
            // recipientType = MessageRecipientType.User;
            message = Message.CreateUserMessage(sender, recipientUser, request.EncryptedContent, MessageType.EncryptedText);
        }
        else if (request.ChatType == Protocol.Enums.ProtocolChatType.Group)
        {
            var recipientGroup = await _unitOfWork.Groups.GetByIdAsync(Guid.Parse(request.RecipientId), cancellationToken);
            if (recipientGroup == null)
            {
                _logger.LogWarning("Recipient group not found: {RecipientId}", request.RecipientId);
                return Result<Guid>.Failure(new Error("Message.Send.RecipientGroupNotFound", "Recipient group not found."));
            }
            // recipientType = MessageRecipientType.Group;
            message = Message.CreateGroupMessage(sender, recipientGroup, request.EncryptedContent, MessageType.EncryptedText);
        }
        else
        {
            _logger.LogError("Invalid chat type: {ChatType}", request.ChatType);
            return Result<Guid>.Failure(new Error("Message.Send.InvalidChatType", "Invalid chat type."));
        }

        await _unitOfWork.MessageRepository.AddAsync(message, cancellationToken); // Changed Messages to MessageRepository and added CancellationToken
        
        // The MessageSentEvent will carry the message object which now has EncryptedText type and encrypted content.
        // The existing MessageSentEventHandler should be able to handle this.
        // We might need to adjust MessageSentEventHandler or ChatNotificationService to send EncryptedMessageNotificationDto
        // if the message type is EncryptedText.
        // Ensure sender and recipientGroup (if applicable) are in scope
        var senderUser = await _unitOfWork.Users.GetByIdAsync(request.SenderUserId, cancellationToken); // Re-fetch or ensure sender is available
        if (senderUser == null)
        {
            // This should ideally not happen if the initial check passed, but as a safeguard:
            _logger.LogError("Sender user not found when creating MessageSentEvent. SenderUserId: {SenderUserId}", request.SenderUserId);
            return Result<Guid>.Failure(new Error("SendEncryptedMessage.SenderNotFound", "Sender not found during event creation."));
        }
        
        string? groupName = null;
        if (message.RecipientType == MessageRecipientType.Group)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(message.RecipientId, cancellationToken);
            groupName = group?.Name;
        }

        // Assuming Message.Content is a suitable preview for encrypted messages.
        // Or, a generic "Encrypted message" could be used.
        string contentPreview = message.Type == MessageType.EncryptedText ? "[Encrypted Message]" : message.Content.Substring(0, Math.Min(message.Content.Length, 100));


        message.AddDomainEvent(new MessageSentEvent(
            message.MessageId,
            senderUser.Id, // Assuming SenderId from message (CreatedBy) is reliable, or use senderUser.Id
            message.RecipientId,
            message.RecipientType,
            contentPreview, // Use a preview or placeholder for encrypted content
            senderUser.Username ?? "Unknown User", // Ensure Username is not null
            senderUser.Profile?.AvatarUrl, // Changed UserProfile to Profile
            groupName
        ));

        await _unitOfWork.CompleteAsync(cancellationToken);

        _logger.LogInformation("Encrypted message sent: {MessageId} from {SenderUserId} to {RecipientId} ({ChatType})", 
            message.Id, request.SenderUserId, request.RecipientId, request.ChatType);

        return Result<Guid>.Success(message.Id);
    }
}