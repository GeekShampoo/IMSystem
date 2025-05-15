using MediatR;
using IMSystem.Server.Domain.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Core.Interfaces.Persistence; // For IMessageRepository, IUnitOfWork
using IMSystem.Server.Domain.Entities;             // For Message entity
using IMSystem.Server.Domain.Enums;                // For MessageType, MessageRecipientType
using System.Text.Json;
using IMSystem.Server.Domain.Events.Files;
using IMSystem.Server.Domain.Events.Messages; // For serializing event for OutboxMessage

namespace IMSystem.Server.Core.Features.Files.Events;

/// <summary>
/// 处理 FileUploadConfirmedEvent 的事件处理器。
/// </summary>
public class FileUploadConfirmedEventHandler : INotificationHandler<FileUploadConfirmedEvent>
{
    private readonly ILogger<FileUploadConfirmedEventHandler> _logger;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;

    public FileUploadConfirmedEventHandler(
        ILogger<FileUploadConfirmedEventHandler> logger,
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IGroupRepository groupRepository)
    {
        _logger = logger;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
    }

    public async Task Handle(FileUploadConfirmedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling FileUploadConfirmedEvent. FileMetadataId: {FileMetadataId}, FileName: {FileName}, UploaderId: {UploaderId}, AccessUrl: {AccessUrl}, ClientMessageId: {ClientMessageId}",
            notification.FileMetadataId, notification.FileName, notification.UploaderId, notification.AccessUrl ?? "N/A", notification.ClientMessageId ?? "N/A");

        if (string.IsNullOrWhiteSpace(notification.ClientMessageId) || notification.UploaderId == Guid.Empty)
        {
            _logger.LogInformation("FileUploadConfirmedEvent for FileMetadataId {FileMetadataId} has no ClientMessageId or UploaderId. No message to update.", notification.FileMetadataId);
            return;
        }

        var messageToUpdate = await _messageRepository.GetByClientMessageIdAndSenderIdAsync(notification.ClientMessageId, notification.UploaderId);

        if (messageToUpdate == null)
        {
            _logger.LogWarning("No message found with ClientMessageId {ClientMessageId} for sender {UploaderId} to associate with FileUploadConfirmedEvent {FileMetadataId}.",
                notification.ClientMessageId, notification.UploaderId, notification.FileMetadataId);
            return;
        }

        // Determine new message type based on file content type
        MessageType newMessageType;
        if (notification.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            newMessageType = MessageType.Image;
        }
        else if (notification.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            newMessageType = MessageType.Audio;
        }
        else if (notification.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            newMessageType = MessageType.Video;
        }
        else
        {
            newMessageType = MessageType.File;
        }

        // Construct new message content, e.g., JSON with file details
        var fileMessageContent = new
        {
            FileId = notification.FileMetadataId,
            FileName = notification.FileName,
            Url = notification.AccessUrl, // This should be the publicly accessible URL
            ContentType = notification.ContentType,
            FileSize = notification.FileSize
        };
        string newContentJson = JsonSerializer.Serialize(fileMessageContent);

        try
        {
            messageToUpdate.UpdateContentAndType(newContentJson, newMessageType, notification.UploaderId);

            // Fetch sender details
            string senderUsername = "Unknown User";
            string? senderAvatarUrl = null;
            if (messageToUpdate.CreatedBy.HasValue && messageToUpdate.CreatedBy.Value != Guid.Empty)
            {
                var sender = await _userRepository.GetByIdAsync(messageToUpdate.CreatedBy.Value);
                if (sender != null)
                {
                    senderUsername = sender.Username;
                    senderAvatarUrl = sender.Profile?.AvatarUrl;
                }
                else
                {
                    _logger.LogWarning("Sender with ID {SenderId} not found for updated message {MessageId}.", messageToUpdate.CreatedBy.Value, messageToUpdate.Id);
                }
            }

            // Fetch group name if it's a group message
            string? groupName = null;
            if (messageToUpdate.RecipientType == MessageRecipientType.Group)
            {
                var group = await _groupRepository.GetByIdAsync(messageToUpdate.RecipientId);
                if (group != null)
                {
                    groupName = group.Name;
                }
                else
                {
                    _logger.LogWarning("Group with ID {GroupId} not found for updated message {MessageId}.", messageToUpdate.RecipientId, messageToUpdate.Id);
                }
            }

            var messageSentEvent = new MessageSentEvent(
                messageId: messageToUpdate.Id,
                senderId: messageToUpdate.CreatedBy!.Value,
                recipientId: messageToUpdate.RecipientId,
                recipientType: messageToUpdate.RecipientType,
                messageContentPreview: $"[{newMessageType}] {notification.FileName}", // Generate a new preview
                senderUsername: senderUsername,
                senderAvatarUrl: senderAvatarUrl,
                groupName: groupName
            );
            
            // 使用实体的AddDomainEvent方法添加事件，由ApplicationDbContext统一处理
            messageToUpdate.AddDomainEvent(messageSentEvent);
            
            // 保存消息，这将触发ApplicationDbContext.DispatchDomainEventsAsync()
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Re-published MessageSentEvent via Domain Event for updated message {MessageId} (ClientMessageId: {ClientMessageId}).",
                 messageToUpdate.Id, notification.ClientMessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating message for ClientMessageId {ClientMessageId} after FileUploadConfirmedEvent {FileMetadataId}.",
                notification.ClientMessageId, notification.FileMetadataId);
        }
    }
}