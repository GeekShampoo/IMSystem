using AutoMapper;
using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services; // For IChatNotificationService
using IMSystem.Server.Domain.Events;
using MediatR;
// using Microsoft.AspNetCore.SignalR; // No longer needed here
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Entities; // For User, Message entities
using IMSystem.Server.Domain.Enums;
using IMSystem.Server.Domain.Events.Messages; // For MessageRecipientType

namespace IMSystem.Server.Core.Features.Messages.Events
{
    public class MessageSentEventHandler : INotificationHandler<MessageSentEvent>
    {
        private readonly IChatNotificationService _chatNotificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<MessageSentEventHandler> _logger;
        // private readonly IUserRepository _userRepository; // No longer needed directly
        private readonly IMessageRepository _messageRepository;
        // private readonly IGroupRepository _groupRepository; // No longer needed directly

        public MessageSentEventHandler(
            IChatNotificationService chatNotificationService,
            IMapper mapper,
            ILogger<MessageSentEventHandler> logger,
            // IUserRepository userRepository, // No longer needed directly
            IMessageRepository messageRepository
            // IGroupRepository groupRepository // No longer needed directly
            )
        {
            _chatNotificationService = chatNotificationService ?? throw new ArgumentNullException(nameof(chatNotificationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            // _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        }

        public async Task Handle(MessageSentEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Handling MessageSentEvent for MessageId: {MessageId}, SenderId: {SenderId} ({SenderUsername}), RecipientId: {RecipientId}, Type: {RecipientType}, GroupName: {GroupName}, Preview: {Preview}",
                notification.MessageId, notification.SenderId, notification.SenderUsername, notification.RecipientId, notification.RecipientType, notification.GroupName ?? "N/A", notification.MessageContentPreview);

            try
            {
                var messageEntity = await _messageRepository.GetByIdAsync(notification.MessageId);
                if (messageEntity == null)
                {
                    _logger.LogError("MessageSentEvent: Message with ID {MessageId} not found in repository. Cannot push to clients.", notification.MessageId);
                    return;
                }

                // 使用 AutoMapper 映射基础信息
                var messageDto = _mapper.Map<MessageDto>(messageEntity);

                // 设置发送者和群组信息（从事件中获取）
                messageDto.SenderUsername = notification.SenderUsername;
                messageDto.SenderAvatarUrl = notification.SenderAvatarUrl;

                if (notification.RecipientType == MessageRecipientType.User)
                {
                    // Ensure GroupName is null for user messages in DTO, even if event had it (though it shouldn't)
                    messageDto.GroupName = null;
                    _logger.LogInformation("Pushing user message {MessageId} to recipient {RecipientId} and sender {SenderId} ({SenderUsername})",
                        notification.MessageId, notification.RecipientId, notification.SenderId, notification.SenderUsername);

                    // 推送给接收者
                    await _chatNotificationService.SendMessageToUserAsync(notification.RecipientId.ToString(), messageDto, cancellationToken);

                    // 推送给发送者 (用于多端同步)
                    await _chatNotificationService.SendMessageToUserAsync(notification.SenderId.ToString(), messageDto, cancellationToken);
                }
                else if (notification.RecipientType == MessageRecipientType.Group)
                {
                    messageDto.GroupName = notification.GroupName; // Already in event
                    // messageDto.GroupId is already mapped by AutoMapper if RecipientType is Group and RecipientId is GroupId

                    if (string.IsNullOrEmpty(messageDto.GroupName))
                    {
                         _logger.LogWarning("MessageSentEvent: GroupName is missing in event for GroupId {GroupId}, MessageId {MessageId}. DTO GroupName will be null or from mapping.", notification.RecipientId, notification.MessageId);
                    }

                    _logger.LogInformation("Pushing group message {MessageId} to group {GroupId} ({GroupName}) (originating sender: {SenderId} ({SenderUsername}))",
                        notification.MessageId, notification.RecipientId, messageDto.GroupName ?? "N/A", notification.SenderId, notification.SenderUsername);
                    
                    await _chatNotificationService.SendMessageToGroupAsync(notification.RecipientId.ToString(), messageDto, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("MessageSentEvent for MessageId {MessageId} has unhandled RecipientType {RecipientType}. Skipping push.",
                        notification.MessageId, notification.RecipientType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling MessageSentEvent for MessageId: {MessageId}. Notification: {@Notification}",
                    notification.MessageId, notification);
            }
        }
    }
}