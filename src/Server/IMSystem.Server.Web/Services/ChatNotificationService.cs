using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.DTOs.Notifications;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using IMSystem.Protocol.Enums;
using IMSystem.Protocol.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Web.Services
{
    /// <summary>
    /// 通知服务实现，提供通过 SignalR 向客户端发送各种通知的功能
    /// 同时实现 INotificationService 和 IChatNotificationService 接口
    /// </summary>
    public class ChatNotificationService : IChatNotificationService, INotificationService
    {
        private readonly IHubContext<MessagingHub> _messagingHubContext;
        private readonly IHubContext<PresenceHub> _presenceHubContext;
        private readonly ILogger<ChatNotificationService> _logger;

        public ChatNotificationService(
            IHubContext<MessagingHub> messagingHubContext,
            IHubContext<PresenceHub> presenceHubContext,
            ILogger<ChatNotificationService> logger)
        {
            _messagingHubContext = messagingHubContext ?? throw new ArgumentNullException(nameof(messagingHubContext));
            _presenceHubContext = presenceHubContext ?? throw new ArgumentNullException(nameof(presenceHubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region INotificationService Implementation

        /// <summary>
        /// 实现 INotificationService.SendNotificationAsync - 发送通知给单个用户
        /// </summary>
        /// <param name="userId">接收通知的用户ID</param>
        /// <param name="notificationType">通知类型，用作 SignalR 方法名</param>
        /// <param name="payload">通知内容</param>
        public async Task SendNotificationAsync(string userId, string notificationType, object payload)
        {
            _logger.LogInformation("Attempting to send notification of type {NotificationType} to user {UserId} via MessagingHub", notificationType, userId);
            try
            {
                // 通知类型用作 SignalR 方法名
                await _messagingHubContext.Clients.User(userId).SendAsync(notificationType, payload);
                _logger.LogInformation("Notification of type {NotificationType} sent to user {UserId} successfully.", notificationType, userId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending notification of type {NotificationType} to user {UserId}", notificationType, userId);
                throw;
            }
        }

        /// <summary>
        /// 实现 INotificationService.SendNotificationToMultipleUsersAsync - 发送通知给多个用户
        /// </summary>
        public async Task SendNotificationToMultipleUsersAsync(IEnumerable<string> userIds, string notificationType, object payload)
        {
            if (userIds == null || !userIds.Any())
            {
                _logger.LogInformation("No recipients for multi-user notification of type {NotificationType}.", notificationType);
                return;
            }
            _logger.LogInformation("Attempting to send notification of type {NotificationType} to {UserCount} users via MessagingHub", notificationType, userIds.Count());
            try
            {
                await _messagingHubContext.Clients.Users(userIds).SendAsync(notificationType, payload);
                _logger.LogInformation("Notification of type {NotificationType} sent to {UserCount} users successfully.", notificationType, userIds.Count());
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending notification of type {NotificationType} to multiple users", notificationType);
                throw;
            }
        }

        /// <summary>
        /// 实现 INotificationService.SendNotificationToGroupAsync - 发送通知给一个组
        /// </summary>
        public async Task SendNotificationToGroupAsync(string groupName, string notificationType, object payload)
        {
            _logger.LogInformation("Attempting to send notification of type {NotificationType} to group {GroupName} via MessagingHub", notificationType, groupName);
            try
            {
                await _messagingHubContext.Clients.Group(groupName).SendAsync(notificationType, payload);
                _logger.LogInformation("Notification of type {NotificationType} sent to group {GroupName} successfully.", notificationType, groupName);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending notification of type {NotificationType} to group {GroupName}", notificationType, groupName);
                throw;
            }
        }

        #endregion

        #region IChatNotificationService Implementation

        /// <summary>
        /// 实现 IChatNotificationService.SendNotificationAsync - 允许使用 CancellationToken 的扩展版本
        /// 这个方法与 INotificationService.SendNotificationAsync 功能相同，但支持取消令牌
        /// </summary>
        /// <param name="userId">接收通知的用户ID</param>
        /// <param name="methodName">通知类型，用作 SignalR 方法名</param>
        /// <param name="payload">通知内容</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task SendNotificationAsync(string userId, string methodName, object payload, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to send notification via method {MethodName} to user {UserId} via MessagingHub", methodName, userId);
            try
            {
                await _messagingHubContext.Clients.User(userId).SendAsync(methodName, payload, cancellationToken);
                _logger.LogInformation("Notification sent via method {MethodName} to user {UserId} successfully.", methodName, userId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending notification via method {MethodName} to user {UserId}", methodName, userId);
                throw;
            }
        }

        public async Task SendMessageToGroupAsync(string groupId, MessageDto message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to send message {MessageId} to group {GroupId} via MessagingHub", message.MessageId, groupId);
            await _messagingHubContext.Clients.Group(groupId).SendAsync(SignalRClientMethods.ReceiveMessage, message, cancellationToken);
            _logger.LogInformation("Message {MessageId} sent to group {GroupId} via MessagingHub", message.MessageId, groupId);
        }

        public async Task SendMessageToUserAsync(string userId, MessageDto message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to send message {MessageId} to user {UserId} via MessagingHub", message.MessageId, userId);
            await _messagingHubContext.Clients.User(userId).SendAsync(SignalRClientMethods.ReceiveMessage, message, cancellationToken);
            _logger.LogInformation("Message {MessageId} sent to user {UserId} via MessagingHub", message.MessageId, userId);
        }

        public async Task NotifyMessageReadAsync(string userIdToNotify, MessageReadNotificationDto notification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to send message read notification for MessageId {MessageId} to user {UserIdToNotify} via MessagingHub", notification.MessageId, userIdToNotify);
            try
            {
                await _messagingHubContext.Clients.User(userIdToNotify).SendAsync(SignalRClientMethods.ReceiveMessageReadNotification, notification, cancellationToken);
                _logger.LogInformation("Message read notification for MessageId {MessageId} sent to user {UserIdToNotify} via MessagingHub", notification.MessageId, userIdToNotify);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending message read notification for MessageId {MessageId} to user {UserIdToNotify}", notification.MessageId, userIdToNotify);
                throw;
            }
        }

        public async Task NotifyMessageEditedAsync(string userIdToNotify, MessageEditedNotificationDto notification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to send message edited notification for MessageId {MessageId} (ChatId: {ChatId}) to user {UserIdToNotify} via MessagingHub",
                notification.MessageId, notification.ChatId, userIdToNotify);
            try
            {
                await _messagingHubContext.Clients.User(userIdToNotify).SendAsync(SignalRClientMethods.ReceiveMessageEditedNotification, notification, cancellationToken);
                _logger.LogInformation("Message edited notification for MessageId {MessageId} (ChatId: {ChatId}) sent to user {UserIdToNotify} via MessagingHub",
                    notification.MessageId, notification.ChatId, userIdToNotify);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending message edited notification for MessageId {MessageId} to user {UserIdToNotify}", notification.MessageId, userIdToNotify);
                throw;
            }
        }

        public async Task NotifyGroupMessageEditedAsync(string groupId, MessageEditedNotificationDto notification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to send message edited notification for MessageId {MessageId} to group {GroupId} (ChatId: {ChatId}) via MessagingHub",
                notification.MessageId, groupId, notification.ChatId);
            try
            {
                await _messagingHubContext.Clients.Group(groupId).SendAsync(SignalRClientMethods.ReceiveMessageEditedNotification, notification, cancellationToken);
                _logger.LogInformation("Message edited notification for MessageId {MessageId} sent to group {GroupId} (ChatId: {ChatId}) via MessagingHub",
                    notification.MessageId, groupId, notification.ChatId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending message edited notification for MessageId {MessageId} to group {GroupId}", notification.MessageId, groupId);
                throw;
            }
        }

        public async Task NotifyUserPresenceChangedAsync(IEnumerable<Guid> recipientUserIds, UserPresenceNotificationPayload presencePayload, CancellationToken cancellationToken = default)
        {
            if (recipientUserIds == null || !recipientUserIds.Any())
            {
                _logger.LogInformation("No recipients for user presence change notification.");
                return;
            }

            _logger.LogInformation("Attempting to send user presence change notification to {RecipientCount} users via PresenceHub.", recipientUserIds.Count());
            var userIdsAsString = recipientUserIds.Select(id => id.ToString()).ToList();

            try
            {
                await _presenceHubContext.Clients.Users(userIdsAsString).SendAsync(SignalRClientMethods.UserPresenceChanged, presencePayload, cancellationToken);
                _logger.LogInformation("User presence change notification sent to {RecipientCount} users successfully.", userIdsAsString.Count);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending user presence change notification.");
                throw;
            }
        }

        public async Task NotifyEncryptedMessageSentAsync(string recipientId, ProtocolChatType chatType, EncryptedMessageNotificationDto notification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to send encrypted message notification for MessageId {MessageId} to {RecipientType} {RecipientId} via MessagingHub",
                notification.MessageId, chatType, recipientId);
            try
            {
                if (chatType == ProtocolChatType.Private)
                {
                    await _messagingHubContext.Clients.User(recipientId).SendAsync(SignalRClientMethods.ReceiveEncryptedMessage, notification, cancellationToken);
                    _logger.LogInformation("Encrypted message notification for MessageId {MessageId} sent to user {UserId} via MessagingHub",
                        notification.MessageId, recipientId);
                }
                else if (chatType == ProtocolChatType.Group)
                {
                    await _messagingHubContext.Clients.Group(recipientId).SendAsync(SignalRClientMethods.ReceiveEncryptedMessage, notification, cancellationToken);
                    _logger.LogInformation("Encrypted message notification for MessageId {MessageId} sent to group {GroupId} via MessagingHub",
                        notification.MessageId, recipientId);
                }
                else
                {
                    _logger.LogWarning("Unsupported chat type {ChatType} for encrypted message notification.", chatType);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending encrypted message notification for MessageId {MessageId} to {RecipientType} {RecipientId}",
                    notification.MessageId, chatType, recipientId);
                throw;
            }
        }

        #endregion
    }
}