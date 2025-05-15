using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.DTOs.Notifications;
using IMSystem.Protocol.DTOs.Requests.Messages;
using IMSystem.Protocol.Enums;
using IMSystem.Protocol.DTOs.Responses.Messages;

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for chat-related operations.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// 通过 HTTP API 发送用户消息。
        /// </summary>
        /// <param name="message">要发送的消息数据。</param>
        /// <returns>一个包含结果的对象，如果成功，则包含服务端生成的消息ID。</returns>
        Task<Result<Guid>> SendUserMessageHttpAsync(SendMessageDto message);
    
        /// <summary>
        /// 通过 HTTP API 发送群组消息。
        /// </summary>
        /// <param name="message">要发送的消息数据。</param>
        /// <returns>一个包含结果的对象，如果成功，则包含服务端生成的消息ID。</returns>
        Task<Result<Guid>> SendGroupMessageHttpAsync(SendMessageDto message);
    
        /// <summary>
        /// 通过 SignalR Hub 发起密钥交换。
        /// </summary>
        /// <param name="request">密钥交换的请求详情。</param>
        /// <returns>一个表示操作结果的对象。</returns>
        Task<Result> InitiateKeyExchangeAsync(InitiateKeyExchangeRequest request);
        /// <summary>
        /// Gets historical messages for a user chat, with pagination.
        /// </summary>
        /// <param name="otherUserId">The ID of the other user in the chat.</param>
        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The number of messages per page.</param>
        /// <param name="beforeSequence">Optional. Retrieves messages before this sequence number.</param>
        /// <param name="limit">Optional. Limits the number of messages if beforeSequence is used.</param>
        /// <returns>A result containing a paged list of messages.</returns>
        Task<Result<PagedResult<MessageDto>>> GetUserMessagesAsync(string otherUserId, int pageNumber, int pageSize, long? beforeSequence = null, int? limit = null);

        /// <summary>
        /// Gets historical messages for a group chat, with pagination.
        /// </summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The number of messages per page.</param>
        /// <param name="beforeSequence">Optional. Retrieves messages before this sequence number.</param>
        /// <param name="limit">Optional. Limits the number of messages if beforeSequence is used.</param>
        /// <returns>A result containing a paged list of messages.</returns>
        Task<Result<PagedResult<MessageDto>>> GetGroupMessagesAsync(string groupId, int pageNumber, int pageSize, long? beforeSequence = null, int? limit = null);

        /// <summary>
        /// Sends a message to a user via SignalR Hub.
        /// </summary>
        /// <param name="message">The message data to send.</param>
        /// <returns>A result containing confirmation of the message being sent.</returns>
        Task<Result<MessageSentConfirmationDto>> SendUserMessageAsync(SendMessageDto message);

        /// <summary>
        /// Sends a message to a group via SignalR Hub.
        /// </summary>
        /// <param name="message">The message data to send.</param>
        /// <returns>A result containing confirmation of the message being sent.</returns>
        Task<Result<MessageSentConfirmationDto>> SendGroupMessageAsync(SendMessageDto message);

        /// <summary>
        /// Marks a specific message as read via SignalR Hub.
        /// </summary>
        /// <param name="messageId">The ID of the message to mark as read.</param>
        /// <param name="chatPartnerId">The ID of the chat partner (user or group).</param>
        /// <param name="chatType">The type of chat (user or group).</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> MarkMessageAsReadAsync(string messageId, string chatPartnerId, ProtocolChatType chatType);

        /// <summary>
        /// Marks multiple messages as read via API.
        /// </summary>
        /// <param name="request">The request containing details of messages to mark as read.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> MarkMessagesAsReadAsync(MarkMessagesAsReadRequest request);

        /// <summary>
        /// Recalls a previously sent message via API.
        /// </summary>
        /// <param name="messageId">The ID of the message to recall.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> RecallMessageAsync(string messageId);

        /// <summary>
        /// Edits a previously sent message via API.
        /// </summary>
        /// <param name="messageId">The ID of the message to edit.</param>
        /// <param name="request">The request containing the edited message content.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> EditMessageAsync(string messageId, EditMessageRequest request);

        /// <summary>
        /// Sends a typing notification to a recipient via SignalR Hub.
        /// </summary>
        /// <param name="recipientId">The ID of the recipient (user or group).</param>
        /// <param name="chatType">The type of chat.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendTypingNotificationAsync(string recipientId, ProtocolChatType chatType);

        /// <summary>
        /// Gets messages after a specific sequence number for a given chat.
        /// </summary>
        /// <param name="chatPartnerId">The ID of the other user or group.</param>
        /// <param name="chatType">The type of chat (User or Group).</param>
        /// <param name="afterSequence">The sequence number after which to fetch messages.</param>
        /// <param name="limit">Optional. Maximum number of messages to fetch.</param>
        /// <returns>A result containing a list of messages.</returns>
        Task<Result<List<MessageDto>>> GetMessagesAfterSequenceAsync(string chatPartnerId, ProtocolChatType chatType, long afterSequence, int? limit = null);
        /// <summary>
        /// Gets the list of users who have read a specific group message.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>A result containing the list of users who have read the message.</returns>
        Task<Result<GetGroupMessageReadUsersResponse>> GetGroupMessageReadByAsync(string messageId);

        /// <summary>
        /// Sends an encrypted message via API.
        /// </summary>
        /// <param name="request">The request containing the encrypted message details.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> SendEncryptedMessageAsync(SendEncryptedMessageRequest request);
    }
}