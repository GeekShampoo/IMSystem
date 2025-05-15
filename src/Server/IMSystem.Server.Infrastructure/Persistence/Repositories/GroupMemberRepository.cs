using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IMSystem.Server.Infrastructure.Persistence.Repositories;

public class GroupMemberRepository : GenericRepository<GroupMember>, IGroupMemberRepository
{
    // _context is inherited from GenericRepository

    public GroupMemberRepository(ApplicationDbContext context) : base(context) // Call base constructor
    {
        // _context = context ?? throw new ArgumentNullException(nameof(context)); // Handled by base
    }

    // IGroupMemberRepository specific methods
    public async Task<GroupMember?> GetMemberOrDefaultAsync(Guid groupId, Guid userId)
    {
        return await _context.GroupMembers
            .Include(gm => gm.User).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
    }

    public async Task<GroupMember?> GetMembershipAsync(Guid groupId, Guid userId)
    {
        // 直接复用已有的 GetMemberOrDefaultAsync 方法，功能相同
        return await GetMemberOrDefaultAsync(groupId, userId);
    }

    public async Task<IEnumerable<GroupMember>> GetMembersByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Include(gm => gm.User).ThenInclude(u => u.Profile)
            .ToListAsync(cancellationToken);
    }

    // Implementation of IGenericRepository<GroupMember>
    public override async Task<GroupMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        // FindAsync can take a CancellationToken directly if the primary key is simple.
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken: cancellationToken); // Use _dbSet from base
    }

    // GetAllAsync is inherited

    public override async Task<IEnumerable<GroupMember>> FindAsync(Expression<Func<GroupMember, bool>> predicate, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet
            .Where(predicate)
            .Include(gm => gm.User).ThenInclude(u => u.Profile)
            .ToListAsync(cancellationToken); // Use _dbSet from base
    }

    public override async Task AddAsync(GroupMember entity, CancellationToken cancellationToken = default) // Added override
    {
        await _dbSet.AddAsync(entity, cancellationToken); // Use _dbSet from base
    }

    // AddRangeAsync is inherited

    public override void Update(GroupMember entity) // Added override
    {
        _dbSet.Update(entity); // Use _dbSet from base
    }

    // UpdateRange is inherited

    public override void Remove(GroupMember entity) // Added override
    {
        _dbSet.Remove(entity); // Use _dbSet from base
    }

    // RemoveRange is inherited
    // Queryable is inherited
    // CountAsync is inherited
    // ExistsAsync is inherited

    public async Task<(IEnumerable<GroupMember> Members, int TotalCount)> GetMembersByGroupIdPagedAsync(
        Guid groupId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1; // Or a default like 10 or 20
        if (pageSize > 100) pageSize = 100; // Max page size limit

        var query = _context.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Include(gm => gm.User) // Include User details for each member
                .ThenInclude(u => u!.Profile); // Include UserProfile from User

        var totalCount = await query.CountAsync(cancellationToken);

        var members = await query
            .OrderBy(gm => gm.User!.Username) // Example ordering: by username
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (members, totalCount);
    }

    public async Task<bool> IsUserMemberOfGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// 获取用户所在的所有群组
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户所在的群组列表</returns>
    public async Task<IList<Group>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Include(gm => gm.Group)
            .Select(gm => gm.Group)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取群组的所有成员
    /// </summary>
    /// <param name="groupId">群组ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>群组成员列表</returns>
    public async Task<IList<GroupMember>> GetGroupMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        // 直接利用现有的 GetMembersByGroupIdAsync 方法，保持代码一致性
        var members = await GetMembersByGroupIdAsync(groupId, cancellationToken);
        return members.ToList();
    }
}