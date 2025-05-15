using IMSystem.Server.Core.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore.Storage; // For IDbContextTransaction
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 工作单元的实现，用于管理事务和仓储。
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _currentTransaction;

        // Lazy-loaded repository instances
        private IUserRepository? _userRepository;
        private IMessageRepository? _messageRepository;
        private IGroupRepository? _groupRepository;
        private IFriendshipRepository? _friendshipRepository;
        private IUserProfileRepository? _userProfileRepository;
        private IFriendGroupRepository? _friendGroupRepository;
        private IGroupMemberRepository? _groupMemberRepository;
        private IMessageReadReceiptRepository? _messageReadReceiptRepository;
        private IFileMetadataRepository? _fileMetadataRepository;
        private IOutboxRepository? _outboxRepository;
        private IGroupInvitationRepository? _groupInvitationRepository;
        private IUserFriendGroupRepository? _userFriendGroupRepository;


        /// <summary>
        /// 初始化 <see cref="UnitOfWork"/> 类的新实例。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Implementing IUnitOfWork properties
        public IUserRepository Users => _userRepository ??= new UserRepository(_context);
        public IMessageRepository MessageRepository => _messageRepository ??= new MessageRepository(_context); // Matches interface
        public IGroupRepository Groups => _groupRepository ??= new GroupRepository(_context);
        public IFriendshipRepository Friendships => _friendshipRepository ??= new FriendshipRepository(_context);
        public IUserProfileRepository UserProfiles => _userProfileRepository ??= new UserProfileRepository(_context);
        public IFriendGroupRepository FriendGroups => _friendGroupRepository ??= new FriendGroupRepository(_context);
        public IGroupMemberRepository GroupMembers => _groupMemberRepository ??= new GroupMemberRepository(_context);
        public IMessageReadReceiptRepository MessageReadReceipts => _messageReadReceiptRepository ??= new MessageReadReceiptRepository(_context);
        public IFileMetadataRepository FileMetadata => _fileMetadataRepository ??= new FileMetadataRepository(_context);
        public IOutboxRepository OutboxMessages => _outboxRepository ??= new OutboxRepository(_context);
        public IGroupInvitationRepository GroupInvitations => _groupInvitationRepository ??= new GroupInvitationRepository(_context);
        public IUserFriendGroupRepository UserFriendGroups => _userFriendGroupRepository ??= new UserFriendGroupRepository(_context);


        /// <inheritdoc/>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                return; // 事务已启动
            }
            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await (_currentTransaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken); // 提交失败时回滚
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        /// <inheritdoc/>
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await (_currentTransaction?.RollbackAsync(cancellationToken) ?? Task.CompletedTask);
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
        {
            return await SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}