using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Enums; // Added for FriendshipStatus
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading; // Added for CancellationToken

namespace IMSystem.Server.Infrastructure.Persistence.Repositories;

public class FriendshipRepository : GenericRepository<Friendship>, IFriendshipRepository
{
    // _context is inherited from GenericRepository

    public FriendshipRepository(ApplicationDbContext context) : base(context) // Call base constructor
    {
        // _context = context ?? throw new ArgumentNullException(nameof(context)); // Handled by base
    }

    public override async Task AddAsync(Friendship entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken); // Use _dbSet from base
    }

    public override async Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet // Use _dbSet from base
            .Include(f => f.Requester).ThenInclude(u => u.Profile)
            .Include(f => f.Addressee).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<Friendship?> GetFriendshipBetweenUsersAsync(Guid userId1, Guid userId2)
    {
        // 使用 CreatedBy 替代 RequesterId 进行查询，因为 RequesterId 是基于 CreatedBy 的计算属性
        return await _context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.CreatedBy == userId1 && f.AddresseeId == userId2) ||
                (f.CreatedBy == userId2 && f.AddresseeId == userId1));
    }

    public async Task<Friendship?> GetFriendshipAsync(Guid userId1, Guid userId2)
    {
        // 直接调用已有的 GetFriendshipBetweenUsersAsync 方法，因为它们功能相同
        return await GetFriendshipBetweenUsersAsync(userId1, userId2);
    }

    public async Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(Guid userId, FriendshipStatus? status = null, int? pageNumber = null, int? pageSize = null)
    {
        // 使用 CreatedBy 替代 RequesterId 进行查询
        var query = _context.Friendships
            .Where(f => f.CreatedBy == userId || f.AddresseeId == userId);

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        query = query
            .Include(f => f.Requester).ThenInclude(u => u.Profile)
            .Include(f => f.Addressee).ThenInclude(u => u.Profile);

        if (pageNumber.HasValue && pageSize.HasValue && pageNumber > 0 && pageSize > 0)
        {
            query = query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(Guid userId, FriendshipStatus? status = null)
    {
        return await GetUserFriendshipsAsync(userId, status, null, null);
    }

    public async Task<IEnumerable<Friendship>> GetPendingFriendRequestsForUserAsync(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        return await _context.Friendships
            .Where(f => f.AddresseeId == userId &&
                         f.Status == FriendshipStatus.Pending &&
                         (f.RequestExpiresAt == null || f.RequestExpiresAt > now)) // Filter out expired requests
            .Include(f => f.Requester).ThenInclude(u => u.Profile) // 通常需要显示请求者信息
            .ToListAsync();
    }
    
    public override async Task<IEnumerable<Friendship>> FindAsync(Expression<Func<Friendship, bool>> predicate, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet // Use _dbSet from base
            .Where(predicate)
            .Include(f => f.Requester).ThenInclude(u => u.Profile)
            .Include(f => f.Addressee).ThenInclude(u => u.Profile)
            .ToListAsync(cancellationToken);
    }

    public override void Update(Friendship friendship) // Added override
    {
        _dbSet.Update(friendship); // Use _dbSet from base
    }

    public override void Remove(Friendship friendship) // Added override
    {
        _dbSet.Remove(friendship); // Use _dbSet from base
    }

    // Methods like GetAllAsync, AddRangeAsync, UpdateRange, RemoveRange, Queryable, CountAsync, ExistsAsync
    // are now inherited from GenericRepository<Friendship> and do not need to be re-implemented here
    // unless specific behavior (like eager loading in GetAllAsync) is desired beyond the base implementation.
    // For now, we assume base implementations are sufficient.
}