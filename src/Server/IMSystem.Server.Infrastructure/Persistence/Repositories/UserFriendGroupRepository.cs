using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions; // Required for Expression
using System.Threading.Tasks;
using System.Threading;

namespace IMSystem.Server.Infrastructure.Persistence.Repositories;

public class UserFriendGroupRepository : GenericRepository<UserFriendGroup>, IUserFriendGroupRepository
{
    // _context is inherited from GenericRepository

    public UserFriendGroupRepository(ApplicationDbContext context) : base(context) // Call base constructor
    {
        // _context = context ?? throw new ArgumentNullException(nameof(context)); // Handled by base
    }

    public override async Task AddAsync(UserFriendGroup entity, CancellationToken cancellationToken = default) // Added override
    {
        await _dbSet.AddAsync(entity, cancellationToken); // Use _dbSet from base
    }

    public override void Remove(UserFriendGroup userFriendGroup) // Added override
    {
        _dbSet.Remove(userFriendGroup); // Use _dbSet from base
    }

    public override async Task<UserFriendGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken: cancellationToken); // Use _dbSet from base
    }

    public async Task<UserFriendGroup?> GetByUserIdAndFriendshipIdAsync(Guid userId, Guid friendshipId)
    {
        // UserId in UserFriendGroup is represented by CreatedBy
        return await _context.UserFriendGroups
            .FirstOrDefaultAsync(ufg => ufg.CreatedBy == userId && ufg.FriendshipId == friendshipId);
    }

    public async Task<UserFriendGroup?> GetByFriendGroupIdAndFriendshipIdAsync(Guid friendGroupId, Guid friendshipId)
    {
        return await _context.UserFriendGroups
            .FirstOrDefaultAsync(ufg => ufg.FriendGroupId == friendGroupId && ufg.FriendshipId == friendshipId);
    }
    
    public async Task<IEnumerable<UserFriendGroup>> GetByFriendGroupIdAsync(Guid friendGroupId)
    {
        return await _context.UserFriendGroups
            .Where(ufg => ufg.FriendGroupId == friendGroupId)
            .Include(ufg => ufg.Friendship) // Optionally include related data
                .ThenInclude(f => f.Requester) // Example of further includes
            .Include(ufg => ufg.Friendship)
                .ThenInclude(f => f.Addressee)
            .ToListAsync();
    }

    public async Task RemoveByFriendshipIdAsync(Guid friendshipId, CancellationToken cancellationToken = default)
    {
        var userFriendGroupsToRemove = await _context.UserFriendGroups
            .Where(ufg => ufg.FriendshipId == friendshipId)
            .ToListAsync(cancellationToken);

        if (userFriendGroupsToRemove.Any())
        {
            _context.UserFriendGroups.RemoveRange(userFriendGroupsToRemove);
        }
    }

    // Implementation for IGenericRepository<UserFriendGroup>
    // GetAllAsync is inherited

    public override async Task<IEnumerable<UserFriendGroup>> FindAsync(Expression<Func<UserFriendGroup, bool>> predicate, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken); // Use _dbSet from base
    }

    // AddRangeAsync is inherited

    public override void Update(UserFriendGroup entity) // Added override
    {
        _dbSet.Update(entity); // Use _dbSet from base
    }

    // UpdateRange is inherited
    // RemoveRange is inherited
    // Queryable is inherited
    // CountAsync is inherited
    // ExistsAsync is inherited
}