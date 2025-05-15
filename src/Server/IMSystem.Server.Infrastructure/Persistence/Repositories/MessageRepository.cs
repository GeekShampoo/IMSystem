using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For Message entity
using IMSystem.Server.Domain.Enums;   // For MessageRecipientType
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading; // Added for CancellationToken

namespace IMSystem.Server.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 消息仓储的实现。
    /// </summary>
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        // _context is inherited from GenericRepository

        /// <summary>
        /// 初始化 <see cref="MessageRepository"/> 类的新实例。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        public MessageRepository(ApplicationDbContext context) : base(context) // Call base constructor
        {
            // _context = context ?? throw new ArgumentNullException(nameof(context)); // Handled by base
        }

        /// <inheritdoc/>
        public override async Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) // Added CancellationToken and override
        {
            return await _dbSet // Use _dbSet from base
                                 .Include(m => m.Sender).ThenInclude(s => s.Profile)
                                 .Include(m => m.RecipientUser).ThenInclude(u => u.Profile)
                                 .Include(m => m.RecipientGroup)
                                 .Include(m => m.ReplyToMessage)
                                 .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<IEnumerable<Message>> GetAllAsync(CancellationToken cancellationToken = default) // 修改方法签名，添加 CancellationToken 参数
        {
            return await _dbSet // Use _dbSet from base
                                 .Include(m => m.Sender).ThenInclude(s => s.Profile)
                                 .ToListAsync(cancellationToken); // 修改为使用 cancellationToken
        }

        /// <inheritdoc/>
        public override async Task<IEnumerable<Message>> FindAsync(Expression<Func<Message, bool>> predicate, CancellationToken cancellationToken = default) // Added CancellationToken and override
        {
            return await _dbSet // Use _dbSet from base
                                 .Where(predicate)
                                 .Include(m => m.Sender).ThenInclude(s => s.Profile)
                                 .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task AddAsync(Message message, CancellationToken cancellationToken = default) // Added override
        {
            await _dbSet.AddAsync(message, cancellationToken); // Use _dbSet from base
        }

        // AddRangeAsync is inherited

        /// <inheritdoc/>
        public override void Update(Message message) // Added override
        {
            _dbSet.Update(message); // Use _dbSet from base
        }

        /// <inheritdoc/>
        public override void Remove(Message message) // Added override
        {
            _dbSet.Remove(message); // Use _dbSet from base
        }

        // RemoveRange is inherited

        // This specific ExistsAsync(Guid id) is not part of IGenericRepository, so it's a specific method for IMessageRepository or can be removed if ExistsAsync(predicate) is preferred.
        // For now, assuming it's a specific method for IMessageRepository. If it should be from IGenericRepository, the predicate version should be used.
        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Messages.AnyAsync(m => m.Id == id);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Message>> GetUnreadMessagesForUserAsync(Guid userId)
        {
            // This method is now re-evaluated to use MessageReadReceipts.
            // It fetches all messages directed to the user that they haven't sent themselves
            // and for which no read receipt exists for that user.
            return await _context.Messages
                .Where(m => m.RecipientType == MessageRecipientType.User &&
                             m.RecipientId == userId &&
                             m.CreatedBy != userId && // Exclude messages sent by the user themselves
                             !_context.MessageReadReceipts.Any(r => r.MessageId == m.Id && r.ReaderUserId == userId))
                .Include(m => m.Sender).ThenInclude(s => s.Profile)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<int> GetUnreadMessageCountForUserAsync(Guid userId)
        {
            // Re-evaluated to use MessageReadReceipts.
            return await _context.Messages
                .CountAsync(m => m.RecipientType == MessageRecipientType.User &&
                                 m.RecipientId == userId &&
                                 m.CreatedBy != userId && // Exclude messages sent by the user themselves
                                 !_context.MessageReadReceipts.Any(r => r.MessageId == m.Id && r.ReaderUserId == userId));
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<Message> Messages, int TotalCount)> GetUserMessagesAsync(
            Guid userId1,
            Guid userId2,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100; // Consistent with query validation

            var query = _context.Messages
                .Where(m => m.RecipientType == MessageRecipientType.User &&
                            ((m.CreatedBy == userId1 && m.RecipientId == userId2) ||
                             (m.CreatedBy == userId2 && m.RecipientId == userId1)));

            var totalCount = await query.CountAsync();

            var messages = await query
                .Include(m => m.Sender)
                    .ThenInclude(s => s!.Profile) // Include sender's profile for avatar etc.
                .OrderByDescending(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (messages, totalCount);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<Message> Messages, int TotalCount)> GetGroupMessagesAsync(
            Guid groupId,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100; // Consistent with query validation

            var query = _context.Messages
                .Where(m => m.RecipientType == MessageRecipientType.Group && m.RecipientId == groupId);

            var totalCount = await query.CountAsync();

            var messages = await query
                .Include(m => m.Sender)
                    .ThenInclude(s => s!.Profile) // Include sender's profile for avatar etc.
                .Include(m => m.RecipientGroup) // Include RecipientGroup for GroupName mapping
                .OrderByDescending(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return (messages, totalCount);
        }

        /// <inheritdoc/>
        public async Task<List<Message>> GetUnreadUserMessagesUpToAsync(
            Guid readerUserId,
            Guid chatPartnerId,
            Guid? upToMessageId,
            DateTimeOffset? lastReadTimestamp,
            CancellationToken cancellationToken)
        {
            var query = _context.Messages
                .Where(m => m.RecipientType == MessageRecipientType.User &&
                            m.RecipientId == readerUserId && // Messages sent TO the reader
                            m.CreatedBy == chatPartnerId &&  // Messages FROM the chat partner
                            !_context.MessageReadReceipts.Any(r => r.MessageId == m.Id && r.ReaderUserId == readerUserId)); // Not already read by the reader

            if (upToMessageId.HasValue)
            {
                var cursorMessage = await _context.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == upToMessageId.Value, cancellationToken);
                if (cursorMessage != null)
                {
                    // Mark messages older than or equal to the cursor message's timestamp as read (inclusive of the cursor message if it's unread)
                    query = query.Where(m => m.CreatedAt <= cursorMessage.CreatedAt);
                }
            }
            else if (lastReadTimestamp.HasValue)
            {
                 // Mark messages older than or equal to the last read timestamp as read
                query = query.Where(m => m.CreatedAt <= lastReadTimestamp.Value);
            }
            // If neither upToMessageId nor lastReadTimestamp is provided, get all unread messages from this chat partner.

            return await query
                .OrderBy(m => m.CreatedAt) // Process older messages first
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<Message>> GetUnreadGroupMessagesUpToAsync(
            Guid readerUserId,
            Guid groupId,
            Guid? upToMessageId,
            DateTimeOffset? lastReadTimestamp,
            CancellationToken cancellationToken)
        {
            var query = _context.Messages
                .Where(m => m.RecipientType == MessageRecipientType.Group &&
                            m.RecipientId == groupId && // Messages sent TO the group
                            m.CreatedBy != readerUserId && // Exclude messages sent by the reader themselves
                            !_context.MessageReadReceipts.Any(r => r.MessageId == m.Id && r.ReaderUserId == readerUserId)); // Not already read by the reader

            if (upToMessageId.HasValue)
            {
                var cursorMessage = await _context.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == upToMessageId.Value, cancellationToken);
                if (cursorMessage != null)
                {
                    query = query.Where(m => m.CreatedAt <= cursorMessage.CreatedAt);
                }
            }
            else if (lastReadTimestamp.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= lastReadTimestamp.Value);
            }
            // If neither upToMessageId nor lastReadTimestamp is provided, get all unread messages in this group for the reader.
            
            return await query
                .OrderBy(m => m.CreatedAt) // Process older messages first
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<Message>> GetMessagesAfterSequenceAsync(
            Guid? senderId,
            Guid? recipientId,
            Guid? groupId,
            long afterSequence,
            int limit)
        {
            var query = _context.Messages.AsQueryable();

            // 根据序列号过滤
            query = query.Where(m => m.SequenceNumber > afterSequence);

            // 应用可选的过滤条件
            if (senderId.HasValue)
            {
                query = query.Where(m => m.CreatedBy == senderId.Value);
            }

            if (recipientId.HasValue)
            {
                // 私聊消息过滤
                // 只返回用户作为收件人的消息，或者用户作为发送者且对方是收件人的消息
                query = query.Where(m => 
                    (m.RecipientType == MessageRecipientType.User && 
                    ((m.RecipientId == recipientId.Value) || 
                     (m.CreatedBy == recipientId.Value && m.RecipientType == MessageRecipientType.User))));
            }

            if (groupId.HasValue)
            {
                // 群聊消息过滤
                query = query.Where(m => m.RecipientType == MessageRecipientType.Group && m.RecipientId == groupId.Value);
            }

            // 限制返回数量并按序列号升序排序
            query = query
                .OrderBy(m => m.SequenceNumber)
                .Take(limit);

            // 包含必要的导航属性
            query = query
                .Include(m => m.Sender).ThenInclude(s => s.Profile)
                .Include(m => m.RecipientUser).ThenInclude(u => u.Profile)
                .Include(m => m.RecipientGroup)
                .Include(m => m.ReplyToMessage);

            return await query.ToListAsync();
        }

        // Implementation for IGenericRepository<Message>
        // UpdateRange is inherited
        // Queryable is inherited
        // CountAsync is inherited
        // ExistsAsync(predicate) is inherited
    
        public async Task<Message?> GetByClientMessageIdAndSenderIdAsync(string clientMessageId, Guid senderId)
        {
            if (string.IsNullOrWhiteSpace(clientMessageId))
            {
                return null;
            }
            return await _context.Messages
                .FirstOrDefaultAsync(m => m.ClientMessageId == clientMessageId && m.CreatedBy == senderId);
        }
    }
}