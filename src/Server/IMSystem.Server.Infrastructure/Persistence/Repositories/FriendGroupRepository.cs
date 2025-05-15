using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions; // Required for Expression
using System.Threading.Tasks;
using System.Threading; // Added for CancellationToken

namespace IMSystem.Server.Infrastructure.Persistence.Repositories;

public class FriendGroupRepository : GenericRepository<FriendGroup>, IFriendGroupRepository
{
    // _context is inherited from GenericRepository

    public FriendGroupRepository(ApplicationDbContext context) : base(context) // Call base constructor
    {
        // _context = context ?? throw new ArgumentNullException(nameof(context)); // Handled by base
    }

    public override async Task AddAsync(FriendGroup group, CancellationToken cancellationToken = default) // Added override
    {
        await _dbSet.AddAsync(group, cancellationToken); // Use _dbSet from base
    }

    public override async Task<FriendGroup?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet // Use _dbSet from base
            .Include(fg => fg.User)
            .FirstOrDefaultAsync(fg => fg.Id == groupId, cancellationToken);
    }

    public async Task<IEnumerable<FriendGroup>> GetByUserIdAsync(Guid userId)
    {
        return await _context.FriendGroups
            .Where(fg => fg.CreatedBy == userId) // FriendGroup.User is linked via CreatedBy
            .OrderBy(fg => fg.Order) // 按 Order 排序
            .ThenBy(fg => fg.Name)   // 再按 Name 排序
            .ToListAsync();
    }

    public async Task<FriendGroup?> GetByNameAndUserIdAsync(string name, Guid userId)
    {
        return await _context.FriendGroups
            .FirstOrDefaultAsync(fg => fg.Name == name && fg.CreatedBy == userId);
    }

    public override void Update(FriendGroup group) // Added override
    {
        _dbSet.Update(group); // Use _dbSet from base
    }

    public override void Remove(FriendGroup group) // Added override
    {
        // 注意：删除分组时，需要考虑 UserFriendGroup 表中的关联记录。
        // EF Core 的级联删除配置或手动删除关联记录。
        // 当前假设级联删除已配置或业务逻辑允许直接删除分组。
        _dbSet.Remove(group); // Use _dbSet from base
    }

    public async Task<FriendGroup?> GetDefaultByUserIdAsync(Guid userId)
    {
        return await _context.FriendGroups
            .FirstOrDefaultAsync(fg => fg.CreatedBy == userId && fg.IsDefault);
    }

    // Implementation for IGenericRepository<FriendGroup>
    // GetAllAsync is inherited
    public override async Task<IEnumerable<FriendGroup>> FindAsync(Expression<Func<FriendGroup, bool>> predicate, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken); // Use _dbSet from base
    }

    // AddRangeAsync is inherited
    // UpdateRange is inherited
    // RemoveRange is inherited
    // Queryable is inherited
    // CountAsync is inherited
    // ExistsAsync is inherited

    public async Task<bool> ExistsByUserIdAndOrderAsync(Guid userId, int order)
    {
        return await _context.FriendGroups.AnyAsync(fg => fg.CreatedBy == userId && fg.Order == order);
    }

    public async Task<FriendGroup?> GetByUserIdAndOrderExcludingGroupIdAsync(Guid userId, int order, Guid excludeGroupId)
    {
        return await _context.FriendGroups
            .FirstOrDefaultAsync(fg =>
                fg.CreatedBy == userId &&
                fg.Order == order &&
                fg.Id != excludeGroupId);
    }
}