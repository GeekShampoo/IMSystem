using IMSystem.Server.Domain.Entities;
using System.Collections.Generic; // Added for IEnumerable
using System.Threading.Tasks;
using System.Threading; // Added for CancellationToken

namespace IMSystem.Server.Core.Interfaces.Persistence
{
    /// <summary>
    /// 定义发件箱消息仓储的接口。
    /// </summary>
    public interface IOutboxRepository // Consider inheriting from IGenericRepository<OutboxMessage>
    {
        /// <summary>
        /// 添加一个发件箱消息。
        /// </summary>
        /// <param name="outboxMessage">要添加的发件箱消息实体。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量添加发件箱消息。
        /// </summary>
        /// <param name="outboxMessages">要添加的发件箱消息实体集合。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task AddRangeAsync(IEnumerable<OutboxMessage> outboxMessages, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取未处理的消息。
        /// </summary>
        /// <param name="batchSize">要获取的最大消息数量。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>未处理的消息集合。</returns>
        Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 将消息标记为已处理。
        /// </summary>
        /// <param name="messageId">消息ID。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 批量将消息标记为已处理。
        /// </summary>
        /// <param name="messageIds">消息ID集合。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task MarkRangeAsProcessedAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 按类型获取消息。
        /// </summary>
        /// <param name="type">消息类型。</param>
        /// <param name="processed">是否已处理，null表示获取所有状态的消息。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>符合条件的消息集合。</returns>
        Task<IEnumerable<OutboxMessage>> GetMessagesByTypeAsync(string type, bool? processed = null, CancellationToken cancellationToken = default);
    }
}