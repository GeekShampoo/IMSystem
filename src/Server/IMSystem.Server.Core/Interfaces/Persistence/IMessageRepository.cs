using IMSystem.Server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence
{
    /// <summary>
    /// 定义消息仓储的接口，用于执行与 <see cref="Message"/> 实体相关的数据库操作。
    /// </summary>
    public interface IMessageRepository : IGenericRepository<Message>
    {
        // GetByIdAsync, GetAllAsync, FindAsync, AddAsync, AddRangeAsync,
        // Update, Remove, RemoveRange, Queryable, CountAsync, ExistsAsync(predicate)
        // are inherited from IGenericRepository<Message>.
        // ExistsAsync(Guid id) is covered by ExistsAsync(m => m.Id == id).

        /// <summary>
        /// 异步获取指定用户的未读消息。
        /// </summary>
        /// <param name="userId">用户的唯一标识符。</param>
        /// <returns>表示异步操作的结果，包含指定用户的未读消息集合。</returns>
        Task<IEnumerable<Message>> GetUnreadMessagesForUserAsync(Guid userId);

        /// <summary>
        /// 异步获取指定用户的未读消息数量。
        /// </summary>
        /// <param name="userId">用户的唯一标识符。</param>
        /// <returns>表示异步操作的结果，包含指定用户的未读消息数量。</returns>
        Task<int> GetUnreadMessageCountForUserAsync(Guid userId);

        /// <summary>
        /// 异步获取两个特定用户之间的消息，支持分页。
        /// </summary>
        /// <param name="userId1">第一个用户的ID。</param>
        /// <param name="userId2">第二个用户的ID。</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A tuple containing the list of messages for the page and the total count of messages between the users.</returns>
        Task<(IEnumerable<Message> Messages, int TotalCount)> GetUserMessagesAsync(
            Guid userId1,
            Guid userId2,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// 异步获取指定群组的消息，支持游标分页。
        /// </summary>
        /// <param name="groupId">群组的ID。</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A tuple containing the list of messages for the page and the total count of messages in the group.</returns>
        Task<(IEnumerable<Message> Messages, int TotalCount)> GetGroupMessagesAsync(
            Guid groupId,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// 异步获取指定用户与特定聊天伙伴之间，在某个消息ID或时间戳之前的所有未读消息。
        /// </summary>
        /// <param name="readerUserId">读取者的用户ID。</param>
        /// <param name="chatPartnerId">聊天伙伴的用户ID。</param>
        /// <param name="upToMessageId">可选，标记已读到此消息ID（不包含此消息）。</param>
        /// <param name="lastReadTimestamp">可选，标记已读到此时间戳（不包含此时间戳）。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>未读消息列表。</returns>
        Task<List<Message>> GetUnreadUserMessagesUpToAsync(
            Guid readerUserId,
            Guid chatPartnerId,
            Guid? upToMessageId,
            DateTimeOffset? lastReadTimestamp,
            CancellationToken cancellationToken);

        /// <summary>
        /// 异步获取指定用户在特定群组中，在某个消息ID或时间戳之前的所有未读消息。
        /// </summary>
        /// <param name="readerUserId">读取者的用户ID。</param>
        /// <param name="groupId">群组的ID。</param>
        /// <param name="upToMessageId">可选，标记已读到此消息ID（不包含此消息）。</param>
        /// <param name="lastReadTimestamp">可选，标记已读到此时间戳（不包含此时间戳）。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>未读消息列表。</returns>
        Task<List<Message>> GetUnreadGroupMessagesUpToAsync(
            Guid readerUserId,
            Guid groupId,
            Guid? upToMessageId,
            DateTimeOffset? lastReadTimestamp,
            CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously gets a message by its client-generated ID and sender ID.
        /// This is useful for finding a placeholder message after a file upload.
        /// </summary>
        /// <param name="clientMessageId">The client-generated message ID.</param>
        /// <param name="senderId">The ID of the sender.</param>
        /// <returns>The message if found; otherwise, null.</returns>
        Task<Message?> GetByClientMessageIdAndSenderIdAsync(string clientMessageId, Guid senderId);

        /// <summary>
        /// 获取指定序列号之后的所有消息。可根据发送者ID、接收者ID或群组ID进行过滤。
        /// </summary>
        /// <param name="senderId">可选的发送者用户ID过滤。</param>
        /// <param name="recipientId">可选的接收者用户ID过滤（用于私聊）。</param>
        /// <param name="groupId">可选的群组ID过滤（用于群聊）。</param>
        /// <param name="afterSequence">获取此序列号之后的消息。</param>
        /// <param name="limit">最大返回消息数量。</param>
        /// <returns>满足条件的消息列表。</returns>
        Task<List<Message>> GetMessagesAfterSequenceAsync(
            Guid? senderId,
            Guid? recipientId,
            Guid? groupId,
            long afterSequence,
            int limit);
    }
}