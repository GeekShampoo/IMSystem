using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions; // Required for Expression
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Infrastructure.Persistence.Repositories;

/// <summary>
/// MessageReadReceipt 仓储的 EF Core 实现。
/// </summary>
public class MessageReadReceiptRepository : GenericRepository<MessageReadReceipt>, IMessageReadReceiptRepository
{
    // _context is inherited from GenericRepository

    public MessageReadReceiptRepository(ApplicationDbContext context) : base(context) // Call base constructor
    {
        // _context = context ?? throw new ArgumentNullException(nameof(context)); // Handled by base
    }

    /// <inheritdoc />
    public override async Task AddAsync(MessageReadReceipt receipt, CancellationToken cancellationToken = default) // Added override
    {
        await _dbSet.AddAsync(receipt, cancellationToken); // Use _dbSet from base
    }
    
    // Keep the CancellationToken version if IMessageReadReceiptRepository explicitly defines it and it's used.
    // For now, assuming the IGenericRepository signature is primary.
    // public async Task AddAsync(MessageReadReceipt receipt, CancellationToken cancellationToken = default)
    // {
    //     await _context.MessageReadReceipts.AddAsync(receipt, cancellationToken);
    // }


    /// <inheritdoc />
    public async Task<bool> HasUserReadMessageAsync(Guid messageId, Guid readerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.MessageReadReceipts
            .AnyAsync(r => r.MessageId == messageId && r.ReaderUserId == readerUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<MessageReadReceipt>> GetReceiptsForMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _context.MessageReadReceipts
            .Where(r => r.MessageId == messageId)
            .ToListAsync(cancellationToken);
    }

    /// &inheritdoc />
    public async Task<IEnumerable<MessageReadReceipt>> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _context.MessageReadReceipts
            .Where(r => r.MessageId == messageId)
            .ToListAsync(cancellationToken); // EF Core will return IEnumerable compatible List
    }

    /// &inheritdoc />
    public async Task<Dictionary<Guid, int>> GetReadCountsForMessagesAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default)
    {
        if (messageIds == null || !messageIds.Any())
        {
            return new Dictionary<Guid, int>();
        }

        // Ensure messageIds are distinct to avoid issues if the input list has duplicates.
        var distinctMessageIds = messageIds.Distinct().ToList();

        return await _context.MessageReadReceipts
            .Where(r => distinctMessageIds.Contains(r.MessageId))
            .GroupBy(r => r.MessageId)
            .Select(g => new { MessageId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);
    }

    // Implementation for IGenericRepository<MessageReadReceipt>
    public override async Task<MessageReadReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken: cancellationToken); // Use _dbSet from base
    }

    // GetAllAsync is inherited

    public override async Task<IEnumerable<MessageReadReceipt>> FindAsync(Expression<Func<MessageReadReceipt, bool>> predicate, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken); // Use _dbSet from base
    }

    // AddRangeAsync is inherited

    public override void Update(MessageReadReceipt entity) // Added override
    {
        _dbSet.Update(entity); // Use _dbSet from base
    }

    // UpdateRange is inherited

    public override void Remove(MessageReadReceipt entity) // Added override
    {
        _dbSet.Remove(entity); // Use _dbSet from base
    }

    // RemoveRange is inherited
    // Queryable is inherited
    // CountAsync is inherited
    // ExistsAsync is inherited
}