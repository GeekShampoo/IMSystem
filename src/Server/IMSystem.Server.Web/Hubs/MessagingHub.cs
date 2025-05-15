using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.DTOs.Notifications; // Added for UserTyping DTOs and other notification DTOs
using IMSystem.Protocol.DTOs.Notifications.Common;
using IMSystem.Protocol.DTOs.Requests.Messages;
using IMSystem.Server.Core.Features.Messages.Commands;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging; // Added for ILogger
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Core.Interfaces.Persistence;
using System;
using System.Threading.Tasks;
using IMSystem.Protocol.Enums; // Added for ProtocolMessageRecipientType and ProtocolChatType
using System.Collections.Generic;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Web.Hubs
{
    [Authorize]
    public class MessagingHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly ILogger<MessagingHub> _logger;
        private readonly IMapper _mapper;
        private readonly IChatNotificationService _chatNotificationService;
        private readonly IMessageRepository _messageRepository;
        private readonly IGroupRepository _groupRepository; // 新注入

        public MessagingHub(
            IMediator mediator,
            ILogger<MessagingHub> logger,
            IMapper mapper,
            IChatNotificationService chatNotificationService,
            IMessageRepository messageRepository,
            IGroupRepository groupRepository) // 添加依赖
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _chatNotificationService = chatNotificationService ?? throw new ArgumentNullException(nameof(chatNotificationService));
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} connected to MessagingHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                _logger.LogInformation("User {UserId} (ConnectionId: {ConnectionId}) added to their personal SignalR group: {UserSpecificGroup}", userId, Context.ConnectionId, userId);

                // 查询未读消息并推送离线消息通知
                var unreadMessages = await _messageRepository.GetUnreadMessagesForUserAsync(Guid.Parse(userId));
                var offlineMessages = _mapper.Map<List<MessageDto>>(unreadMessages);
                var dto = new OfflineMessageNotificationDto
                {
                    UserId = Guid.Parse(userId),
                    Messages = offlineMessages,
                    PushTime = DateTimeOffset.UtcNow
                };
                await _chatNotificationService.SendNotificationAsync(userId, SignalRClientMethods.ReceiveOfflineMessages, dto);

                // 自动加入所有已加入的群组
                var groupIds = await _groupRepository.GetGroupIdsForUserAsync(Guid.Parse(userId));
                foreach (var gid in groupIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, gid.ToString());
                    _logger.LogInformation("User {UserId} added to group {GroupId} on connect", userId, gid);
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} disconnected from MessagingHub. ConnectionId: {ConnectionId}. Exception: {ExceptionMessage}", userId, Context.ConnectionId, exception?.Message);
                // Remove from user-specific group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                _logger.LogInformation("User {UserId} (ConnectionId: {ConnectionId}) removed from their personal SignalR group: {UserSpecificGroup}", userId, Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client calls this method to send a direct user-to-user message.
        /// </summary>
        public async Task SendUserMessage(SendMessageDto messageDto)
        {
            var senderUserIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderUserIdString))
            {
                _logger.LogWarning("SendUserMessage called by unauthenticated user or user without identifier.");
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Authentication required to send messages.");
                return;
            }

            if (!Guid.TryParse(senderUserIdString, out var senderGuid))
            {
                _logger.LogWarning("SendUserMessage: Invalid SenderId format from Hub context: {SenderIdString}", senderUserIdString);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Invalid user identifier.");
                return;
            }

            _logger.LogInformation("User {SenderUserId} sending user message to {RecipientId} ({RecipientType}): {ContentPreview}",
                senderUserIdString, messageDto.RecipientId, messageDto.RecipientType, messageDto.Content.Substring(0, Math.Min(messageDto.Content.Length, 50)));

            // messageDto.RecipientType is now ProtocolMessageRecipientType enum
            if (messageDto.RecipientType != ProtocolMessageRecipientType.User)
            {
                _logger.LogWarning("SendUserMessage in MessagingHub received non-User recipient type: {RecipientType}. This method is for user-to-user messages.", messageDto.RecipientType);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, $"Unsupported recipient type '{messageDto.RecipientType}' for SendUserMessage. Expected '{ProtocolMessageRecipientType.User}'.");
                return;
            }

            var command = _mapper.Map<SendMessageCommand>(messageDto);
            command.SenderId = senderGuid;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User message from {SenderId} to {RecipientId} processed successfully by command handler.", senderUserIdString, messageDto.RecipientId);
                var confirmationDto = new MessageSentConfirmationDto
                {
                    ClientMessageId = messageDto.ClientMessageId,
                    Status = "Sent",
                    ServerMessageId = result.Value
                };
                await Clients.Caller.SendAsync(SignalRClientMethods.MessageSentConfirmation, confirmationDto);
            }
            else
            {
                _logger.LogWarning("Failed to process message from {SenderId}. Error: {ErrorCode} - {ErrorMessage}", senderUserIdString, result.Error.Code, result.Error.Message);
                var errorDto = new SignalRErrorDto(result.Error.Code, result.Error.Message);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, errorDto);
            }
        }

        /// <summary>
        /// Client calls this method to send a group message.
        /// </summary>
        public async Task SendGroupMessage(SendMessageDto messageDto)
        {
            var senderUserIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderUserIdString))
            {
                _logger.LogWarning("SendGroupMessage called by unauthenticated user or user without identifier.");
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Authentication required to send group messages.");
                return;
            }

            if (!Guid.TryParse(senderUserIdString, out var senderGuid))
            {
                _logger.LogWarning("SendGroupMessage: Invalid SenderId format from Hub context: {SenderIdString}", senderUserIdString);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Invalid user identifier.");
                return;
            }

            _logger.LogInformation("User {SenderUserId} sending group message to GroupId {GroupId} ({RecipientType}): {ContentPreview}",
                senderUserIdString, messageDto.RecipientId, messageDto.RecipientType, messageDto.Content.Substring(0, Math.Min(messageDto.Content.Length, 50)));

            // messageDto.RecipientType is now ProtocolMessageRecipientType enum
            if (messageDto.RecipientType != ProtocolMessageRecipientType.Group)
            {
                _logger.LogWarning("SendGroupMessage in MessagingHub received non-Group recipient type: {RecipientType}. This method is for group messages.", messageDto.RecipientType);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, $"Invalid recipient type '{messageDto.RecipientType}' for SendGroupMessage. Expected '{ProtocolMessageRecipientType.Group}'.");
                return;
            }

            var command = _mapper.Map<SendMessageCommand>(messageDto);
            command.SenderId = senderGuid;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Group message from {SenderId} to Group {GroupId} processed successfully by command handler.", senderUserIdString, messageDto.RecipientId);
                var confirmationDto = new MessageSentConfirmationDto
                {
                    ClientMessageId = messageDto.ClientMessageId,
                    Status = "SentToGroup",
                    ServerMessageId = result.Value
                };
                await Clients.Caller.SendAsync(SignalRClientMethods.MessageSentConfirmation, confirmationDto);
            }
            else
            {
                _logger.LogWarning("Failed to process message from {SenderId}. Error: {ErrorCode} - {ErrorMessage}", senderUserIdString, result.Error.Code, result.Error.Message);
                var errorDto = new SignalRErrorDto(result.Error.Code, result.Error.Message);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, errorDto);
            }
        }

        /// <summary>
        /// Client calls this method to mark a specific message as read.
        /// </summary>
        public async Task MarkMessageAsRead(MarkMessagesAsReadRequest request) // Changed parameter type
        {
            var readerUserIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(readerUserIdString) || !Guid.TryParse(readerUserIdString, out var readerUserId))
            {
                _logger.LogWarning("MarkMessageAsRead called by unauthenticated user or user with invalid identifier.");
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Authentication required to mark messages as read.");
                return;
            }

            // request.ChatType is now ProtocolChatType enum, which is not nullable.
            // The string.IsNullOrEmpty check is no longer applicable in the same way.
            // The enum will always have a value. We'll validate its specific values later.
            if (request == null || request.ChatId == Guid.Empty) // Removed ChatType check here, will check specific enum values below
            {
                _logger.LogWarning("MarkMessageAsRead called with invalid request data by user {ReaderUserId}. ChatId is required.", readerUserIdString);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Invalid request: ChatId and ChatType are required.");
                return;
            }

            _logger.LogInformation("User {ReaderUserId} attempting to mark messages in chat {ChatId} (Type: {ChatType}) as read in MessagingHub. UpToMessageId: {UpToMessageId}, LastReadTimestamp: {LastReadTimestamp}",
                readerUserIdString, request.ChatId, request.ChatType, request.UpToMessageId, request.LastReadTimestamp);

            Guid? chatPartnerId = null;
            Guid? groupId = null;

            // request.ChatType is now ProtocolChatType enum
            if (request.ChatType == ProtocolChatType.Private) // Assuming "User" chat maps to "Private"
            {
                chatPartnerId = request.ChatId;
            }
            else if (request.ChatType == ProtocolChatType.Group)
            {
                groupId = request.ChatId;
            }
            else
            {
                _logger.LogWarning("MarkMessageAsRead called with invalid ChatType '{ChatType}' by user {ReaderUserId}.", request.ChatType, readerUserIdString);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, $"Invalid ChatType: {request.ChatType}. Must be '{ProtocolChatType.Private}' or '{ProtocolChatType.Group}'.");
                return;
            }

            var command = new MarkMessageAsReadCommand(readerUserId, chatPartnerId, groupId, request.UpToMessageId, request.LastReadTimestamp);
            
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Messages in chat {ChatId} (Type: {ChatType}) successfully marked as read by user {ReaderUserId} via MessagingHub.", request.ChatId, request.ChatType, readerUserIdString);
                // Confirmation to caller.
                // await Clients.Caller.SendAsync("MessagesMarkedAsReadConfirmation", new { request.ChatId, request.ChatType, Status = "Read" });
            }
            else
            {
                _logger.LogWarning("Failed to mark messages in chat {ChatId} (Type: {ChatType}) as read by user {ReaderUserId} via MessagingHub. Error: {Error}", request.ChatId, request.ChatType, readerUserIdString, result.Error);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, $"Failed to mark messages as read: {result.Error}");
            }
        }

        /// <summary>
        /// Client calls this method to send a typing notification.
        /// </summary>
        [Authorize]
        public async Task SendTypingNotification(UserTypingRequestDto request)
        {
            var currentUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("SendTypingNotification called by unauthenticated user or user without identifier.");
                // Optionally, send an error back to the caller, though for typing notifications, it might be less critical.
                return;
            }

            _logger.LogInformation("User {UserId} is {IsTyping} in chat {ChatId} (Type: {ChatType})",
                currentUserId, request.IsTyping ? "typing" : "stopping typing", request.ChatId, request.ChatType);

            var broadcastDto = new UserTypingBroadcastDto
            {
                ChatId = request.ChatId,
                ChatType = request.ChatType,
                UserId = currentUserId,
                IsTyping = request.IsTyping
            };

            if (request.ChatType == ProtocolChatType.Private)
            {
                // Send to the specific user (the other participant in the private chat)
                // The ChatId in this case is the UserID of the recipient.
                await Clients.User(request.ChatId).SendAsync(SignalRClientMethods.ReceiveTypingNotification, broadcastDto);
                _logger.LogInformation("Sent typing notification from {SenderId} to user {RecipientId}", currentUserId, request.ChatId);
            }
            else if (request.ChatType == ProtocolChatType.Group)
            {
                // Send to all other members in the group
                // The ChatId in this case is the GroupID.
                await Clients.OthersInGroup(request.ChatId).SendAsync(SignalRClientMethods.ReceiveTypingNotification, broadcastDto);
                _logger.LogInformation("Sent typing notification from {SenderId} to group {GroupId}", currentUserId, request.ChatId);
            }
            else
            {
                _logger.LogWarning("SendTypingNotification called with invalid ChatType '{ChatType}' by user {UserId}.", request.ChatType, currentUserId);
                // Optionally, inform the caller about the invalid chat type.
            }
        }

        /// <summary>
        /// Initiates a key exchange with another user.
        /// </summary>
        /// <param name="request">The key exchange initiation request.</param>
        public async Task InitiateKeyExchange(InitiateKeyExchangeRequest request)
        {
            var senderUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderUserId))
            {
                _logger.LogWarning("InitiateKeyExchange called by unauthenticated user.");
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Authentication required for key exchange.");
                return;
            }

            if (string.IsNullOrEmpty(request.RecipientUserId) || string.IsNullOrEmpty(request.PublicKey))
            {
                _logger.LogWarning("InitiateKeyExchange called with invalid parameters by user {SenderUserId}. RecipientUserId: {RecipientUserId}, PublicKey provided: {PublicKeyProvided}",
                    senderUserId, request.RecipientUserId, !string.IsNullOrEmpty(request.PublicKey));
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "RecipientUserId and PublicKey are required for key exchange.");
                return;
            }

            if (senderUserId == request.RecipientUserId)
            {
                _logger.LogWarning("User {SenderUserId} attempted to initiate key exchange with themselves.", senderUserId);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Cannot initiate key exchange with yourself.");
                return;
            }

            _logger.LogInformation("User {SenderUserId} initiating key exchange with user {RecipientUserId}.", senderUserId, request.RecipientUserId);

            var offerDto = new KeyExchangeOfferDto
            {
                SenderUserId = senderUserId,
                PublicKey = request.PublicKey
            };

            // Send the offer to the recipient user's specific group (all their connections)
            await Clients.User(request.RecipientUserId).SendAsync(SignalRClientMethods.ReceiveKeyExchangeOffer, offerDto);

            _logger.LogInformation("Key exchange offer sent from {SenderUserId} to {RecipientUserId}.", senderUserId, request.RecipientUserId);
            // Optionally, send a confirmation back to the caller
            // await Clients.Caller.SendAsync("KeyExchangeOfferSent", new { RecipientUserId = request.RecipientUserId });
        }

        /// <summary>
        /// Sends an end-to-end encrypted message.
        /// </summary>
        /// <param name="request">The encrypted message request.</param>
        public async Task SendEncryptedMessage(SendEncryptedMessageRequest request)
        {
            var senderUserIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderUserIdString) || !Guid.TryParse(senderUserIdString, out var senderUserId))
            {
                _logger.LogWarning("SendEncryptedMessage called by unauthenticated user or user with invalid identifier.");
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Authentication required to send encrypted messages.");
                return;
            }

            if (string.IsNullOrEmpty(request.RecipientId) || string.IsNullOrEmpty(request.EncryptedContent))
            {
                _logger.LogWarning("SendEncryptedMessage called with invalid parameters by user {SenderUserId}. RecipientId: {RecipientId}, EncryptedContent provided: {EncryptedContentProvided}",
                    senderUserIdString, request.RecipientId, !string.IsNullOrEmpty(request.EncryptedContent));
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "RecipientId, ChatType, and EncryptedContent are required.");
                return;
            }
            
            // Basic validation for RecipientId if it's supposed to be a Guid
            if (!Guid.TryParse(request.RecipientId, out _))
            {
                 _logger.LogWarning("SendEncryptedMessage called with invalid RecipientId format by user {SenderUserId}. RecipientId: {RecipientId}",
                    senderUserIdString, request.RecipientId);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, "Invalid RecipientId format.");
                return;
            }


            _logger.LogInformation("User {SenderUserId} sending encrypted message to {RecipientId} (Type: {ChatType}).",
                senderUserIdString, request.RecipientId, request.ChatType);

            var command = new SendEncryptedMessageCommand
            {
                SenderUserId = senderUserId,
                RecipientId = request.RecipientId,
                ChatType = request.ChatType,
                EncryptedContent = request.EncryptedContent
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Encrypted message from {SenderUserId} to {RecipientId} (Type: {ChatType}) processed successfully. MessageId: {MessageId}",
                    senderUserIdString, request.RecipientId, request.ChatType, result.Value);
                // Confirmation to caller, including the server-generated MessageId
                var confirmationDto = new MessageSentConfirmationDto
                {
                    ServerMessageId = result.Value,
                    Status = "Sent"
                };
                await Clients.Caller.SendAsync(SignalRClientMethods.EncryptedMessageSentConfirmation, confirmationDto);
            }
            else
            {
                _logger.LogWarning("Failed to process encrypted message from {SenderUserId} to {RecipientId} (Type: {ChatType}). Error: {Error}",
                    senderUserIdString, request.RecipientId, request.ChatType, result.Error);
                await Clients.Caller.SendAsync(SignalRClientMethods.ReceiveError, $"Failed to send encrypted message: {result.Error.Message}");
            }
        }
        /// <summary>
        /// 服务端推送离线消息通知（批量）
        /// </summary>
    }
}