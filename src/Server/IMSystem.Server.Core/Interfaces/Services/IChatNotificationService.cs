using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.DTOs.Notifications;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Services
{
    /// <summary>
    /// 定义聊天通知服务的接口，用于将消息推送到客户端。
    /// </summary>
    public interface IChatNotificationService : INotificationService
    {
        /// <summary>
        /// 将消息发送给指定的用户。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <param name="message">要发送的消息 DTO。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task SendMessageToUserAsync(string userId, MessageDto message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将消息发送给指定的群组。
        /// </summary>
        /// <param name="groupId">群组ID。</param>
        /// <param name="message">要发送的消息 DTO。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task SendMessageToGroupAsync(string groupId, MessageDto message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 通知指定用户某条消息已被读取。
        /// </summary>
        /// <param name="userIdToNotify">要通知的用户ID。</param>
        /// <param name="notification">消息已读通知的 DTO。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task NotifyMessageReadAsync(string userIdToNotify, MessageReadNotificationDto notification, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 向指定用户发送通用通知。
        /// </summary>
        /// <param name="userId">目标用户ID。</param>
        /// <param name="methodName">客户端Hub上要调用的方法名。</param>
        /// <param name="payload">要发送的数据负载。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task SendNotificationAsync(string userId, string methodName, object payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies specified users about a presence change of another user.
        /// </summary>
        /// <param name="recipientUserIds">A collection of user IDs to notify.</param>
        /// <param name="presencePayload">An object containing presence information.</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task NotifyUserPresenceChangedAsync(IEnumerable<System.Guid> recipientUserIds, UserPresenceNotificationPayload presencePayload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies a specific user that a message they are involved in has been edited.
        /// </summary>
        /// <param name="userIdToNotify">The ID of the user to notify.</param>
        /// <param name="notification">The message edited notification DTO.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task NotifyMessageEditedAsync(string userIdToNotify, MessageEditedNotificationDto notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies all members of a group that a message in their group chat has been edited.
        /// </summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="notification">The message edited notification DTO.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task NotifyGroupMessageEditedAsync(string groupId, MessageEditedNotificationDto notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies a recipient (user or group) that an encrypted message has been sent.
        /// </summary>
        /// <param name="recipientId">The ID of the recipient (user or group).</param>
        /// <param name="chatType">The type of chat (User or Group).</param>
        /// <param name="notification">The encrypted message notification DTO.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task NotifyEncryptedMessageSentAsync(string recipientId, Protocol.Enums.ProtocolChatType chatType, EncryptedMessageNotificationDto notification, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Payload for user presence change notifications.
    /// </summary>
    public class UserPresenceNotificationPayload
    {
        /// <summary>
        /// The ID of the user whose presence changed.
        /// </summary>
        public System.Guid UserId { get; set; }

        /// <summary>
        /// Indicates if the user is currently online.
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// The user's custom status message, if any.
        /// </summary>
        public string? CustomStatus { get; set; }

        /// <summary>
        /// The last time the user was seen online, if applicable.
        /// </summary>
        public System.DateTimeOffset? LastSeenAt { get; set; }
    }
}