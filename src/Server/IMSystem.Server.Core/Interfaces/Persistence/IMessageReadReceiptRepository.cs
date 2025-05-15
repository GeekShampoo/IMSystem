using IMSystem.Server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence;

/// <summary>
/// 定义消息已读回执仓储的操作。
/// </summary>
public interface IMessageReadReceiptRepository : IGenericRepository<MessageReadReceipt>
{
   /// <summary>
   /// 添加一个新的消息已读回执。
   /// This method is already covered by IGenericRepository.AddAsync(T entity)
   /// but kept here for explicitness if specific logic or CancellationToken is always desired.
   /// If IGenericRepository.AddAsync is sufficient, this can be removed.
   /// For now, we assume IGenericRepository.AddAsync is sufficient.
   // Task AddAsync(MessageReadReceipt receipt, CancellationToken cancellationToken = default);

   /// <summary>
   /// 检查指定用户是否已经读取了某条消息。
    /// </summary>
    /// <param name="receipt">要添加的已读回执实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    // Task AddAsync(MessageReadReceipt receipt, CancellationToken cancellationToken = default); // Covered by IGenericRepository

    /// <summary>
    /// 检查指定用户是否已经读取了某条消息。
    /// </summary>
    /// <param name="messageId">消息ID。</param>
    /// <param name="readerUserId">读取者用户ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>如果已读则返回 true，否则返回 false。</returns>
    Task<bool> HasUserReadMessageAsync(Guid messageId, Guid readerUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定消息的所有已读回执。
    /// </summary>
    /// <param name="messageId">消息ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已读回执列表。</returns>
    Task<List<MessageReadReceipt>> GetReceiptsForMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定消息的所有已读回执。
    /// </summary>
    /// <param name="messageId">消息ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>一个包含已读回执的可枚举集合。</returns>
    Task<IEnumerable<MessageReadReceipt>> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取一组消息的已读数量。
    /// </summary>
    /// <param name="messageIds">消息ID的集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>一个字典，键是消息ID，值是对应的已读数量。</returns>
    Task<Dictionary<Guid, int>> GetReadCountsForMessagesAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default);

    // 根据需要可以添加更多方法，例如:
    // Task<List<MessageReadReceipt>> GetReceiptsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    // Task<int> GetReadCountForMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
}