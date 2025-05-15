using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence
{
    /// <summary>
    /// 定义工作单元的接口，用于管理事务和聚合仓储操作。
    /// 工作单元模式用于将多个数据操作组合成一个原子操作。
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; } // Consider renaming to UserRepository for consistency if other handlers use that
        IMessageRepository MessageRepository { get; } // Renamed from Messages
        IGroupRepository Groups { get; } // Consider renaming to GroupRepository
        IFriendshipRepository Friendships { get; }
        IUserProfileRepository UserProfiles { get; }
        IFriendGroupRepository FriendGroups { get; }
        IGroupMemberRepository GroupMembers { get; }
        IMessageReadReceiptRepository MessageReadReceipts { get; }
        IFileMetadataRepository FileMetadata { get; }
        IOutboxRepository OutboxMessages { get; }
        IGroupInvitationRepository GroupInvitations { get; }
        IUserFriendGroupRepository UserFriendGroups { get; }
        // ... 其他仓储


        /// <summary>
        /// 异步保存所有更改到数据存储。
        /// </summary>
        /// <param name="cancellationToken">用于监视取消请求的令牌。默认值为 <see cref="CancellationToken.None"/>。</param>
        /// <returns>表示异步保存操作的任务。任务结果包含写入数据存储的状态条目数。</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步完成所有更改，通常是调用 SaveChangesAsync 的别名。
        /// </summary>
        /// <param name="cancellationToken">用于监视取消请求的令牌。默认值为 <see cref="CancellationToken.None"/>。</param>
        /// <returns>表示异步操作的任务。任务结果包含写入数据存储的状态条目数。</returns>
        Task<int> CompleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步开始一个新的数据库事务。
        /// </summary>
        /// <param name="cancellationToken">用于监视取消请求的令牌。默认值为 <see cref="CancellationToken.None"/>。</param>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步提交当前数据库事务。
        /// </summary>
        /// <param name="cancellationToken">用于监视取消请求的令牌。默认值为 <see cref="CancellationToken.None"/>。</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步回滚当前数据库事务。
        /// </summary>
        /// <param name="cancellationToken">用于监视取消请求的令牌。默认值为 <see cref="CancellationToken.None"/>。</param>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        // 如果需要，可以添加一个方法来获取特定类型的仓储实例
        // TRepository GetRepository<TEntity, TRepository>() where TEntity : class where TRepository : class;
    }
}