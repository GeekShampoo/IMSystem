using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using IMSystem.Protocol.DTOs.Notifications;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Messages;
using MediatR;
using Microsoft.Extensions.Logging;
using IMSystem.Server.Domain.Enums; // Required for MessageRecipientType

namespace IMSystem.Server.Core.Features.Messages.EventHandlers
{
    /// <summary>
    /// Handles the <see cref="MessageEditedEvent"/> to notify clients via SignalR.
    /// </summary>
    public class MessageEditedEventHandler : INotificationHandler<MessageEditedEvent>
    {
        private readonly IChatNotificationService _chatNotificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<MessageEditedEventHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEditedEventHandler"/> class.
        /// </summary>
        /// <param name="chatNotificationService">The chat notification service.</param>
        /// <param name="mapper">The AutoMapper instance.</param>
        /// <param name="logger">The logger.</param>
        public MessageEditedEventHandler(
            IChatNotificationService chatNotificationService,
            IMapper mapper,
            ILogger<MessageEditedEventHandler> logger)
        {
            _chatNotificationService = chatNotificationService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Handles the <see cref="MessageEditedEvent"/>.
        /// </summary>
        /// <param name="notification">The message edited event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task Handle(MessageEditedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling MessageEditedEvent for message ID: {MessageId}", notification.EditedMessage.Id);

            var editedMessage = notification.EditedMessage;

            var notificationDto = new MessageEditedNotificationDto
            {
                MessageId = editedMessage.Id,
                ChatId = (editedMessage.RecipientType == MessageRecipientType.User
                            ? (editedMessage.SenderId == editedMessage.RecipientId ? editedMessage.SenderId?.ToString() : editedMessage.RecipientId.ToString())
                            : editedMessage.RecipientId.ToString()) ?? string.Empty, // Ensure ChatId is not null
                Content = editedMessage.Content,
                EditedAt = editedMessage.LastModifiedAt ?? editedMessage.SentAt, // Prefer LastModifiedAt
                EditedByUserId = editedMessage.SenderId?.ToString() ?? string.Empty // Assuming the original sender is the editor, ensure not null
            };
            
            // Determine target clients for notification
            if (editedMessage.RecipientType == MessageRecipientType.User)
            {
                // For private messages, notify both sender and recipient
                // (Sender gets confirmation, recipient gets the update)
                // The ChatId for user messages is typically the other user's ID.
                // If the message is sent from A to B, ChatId for A is B, and for B is A.
                // The notification DTO's ChatId should reflect this.
                // For simplicity, we'll use the RecipientId as the ChatId for the notification DTO
                // and let the ChatNotificationService handle targeting.
                // However, the current DTO's ChatId is a single string.
                // Let's assume ChatId in DTO is the conversation identifier.
                // For user-to-user, it could be a composite key or one of the user IDs depending on context.
                // For now, using RecipientId for user chats, assuming it's the other user.
                // The service needs to know who to send it to.

                // The notificationDto.ChatId should be the identifier of the chat.
                // For a 1-on-1 chat between UserA and UserB, this could be UserA's ID when sending to UserB,
                // and UserB's ID when sending to UserA.
                // Or, it could be a combined chat ID.
                // Let's adjust the ChatId for the notification DTO to be the *other* user.
                // The EditedMessage.RecipientId is the *other* user if it's a 1-on-1 chat.
                // The EditedMessage.SenderId is the one who sent (and edited) it.

                notificationDto.ChatId = editedMessage.RecipientId.ToString(); // The other user in the private chat
                if(editedMessage.SenderId.HasValue)
                    await _chatNotificationService.NotifyMessageEditedAsync(editedMessage.SenderId.Value.ToString(), notificationDto); // Notify editor
                await _chatNotificationService.NotifyMessageEditedAsync(editedMessage.RecipientId.ToString(), notificationDto); // Notify the other participant
            }
            else if (editedMessage.RecipientType == MessageRecipientType.Group)
            {
                // For group messages, notify all members of the group
                // RecipientId is the GroupId
                notificationDto.ChatId = editedMessage.RecipientId.ToString(); // GroupId
                await _chatNotificationService.NotifyGroupMessageEditedAsync(editedMessage.RecipientId.ToString(), notificationDto);
            }
            else
            {
                _logger.LogWarning("Unhandled RecipientType {RecipientType} for MessageEditedEvent on message {MessageId}",
                    editedMessage.RecipientType, editedMessage.Id);
            }

            _logger.LogInformation("Successfully processed MessageEditedEvent for message ID: {MessageId} and sent notifications.", notification.EditedMessage.Id);
        }
    }
}