using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Infrastructure.Persistence.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public OutboxRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken = default)
        {
            await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<OutboxMessage> outboxMessages, CancellationToken cancellationToken = default)
        {
            await _dbContext.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
        }
        
        public async Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            return await _dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.OccurredAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var message = await _dbContext.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
            if (message != null)
            {
                message.MarkAsProcessed();
            }
        }

        public async Task MarkRangeAsProcessedAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default)
        {
            var messages = await _dbContext.OutboxMessages
                .Where(m => messageIds.Contains(m.Id))
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                message.MarkAsProcessed();
            }
        }

        public async Task<IEnumerable<OutboxMessage>> GetMessagesByTypeAsync(string type, bool? processed = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.OutboxMessages.AsQueryable();
            
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(m => m.EventType == type);
            }
            
            if (processed.HasValue)
            {
                query = processed.Value ? query.Where(m => m.ProcessedAt != null)
                                        : query.Where(m => m.ProcessedAt == null);
            }
            
            return await query
                .OrderBy(m => m.OccurredAt)
                .ToListAsync(cancellationToken);
        }
    }
}