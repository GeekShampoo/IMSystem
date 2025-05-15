using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Infrastructure.Persistence; // For ApplicationDbContext
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions; // Required for Expression
using System.Threading.Tasks;
using System.Threading; // Added for CancellationToken

namespace IMSystem.Server.Infrastructure.Persistence.Repositories
{
    public class GroupRepository : GenericRepository<Group>, IGroupRepository
    {
        // _context is inherited from GenericRepository

        public GroupRepository(ApplicationDbContext context) : base(context) // Call base constructor
        {
            // _context = context ?? throw new ArgumentNullException(nameof(context)); // Handled by base
        }

        public override async Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) // Added CancellationToken and override
        {
            // Example of including related entities if needed for this specific GetByIdAsync
            return await _dbSet // Use _dbSet from base
                                 .Include(g => g.Owner) // Example include
                                 .Include(g => g.Members) // Example include
                                     .ThenInclude(gm => gm.User) // Example nested include
                                 .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
            // return await _dbSet.FirstOrDefaultAsync(g => g.Id == id, cancellationToken); // Simpler version without includes
        }

        // GetAllAsync is inherited. Override if includes are always needed.
        /*
        public override async Task<IEnumerable<Group>> GetAllAsync()
        {
            return await _dbSet.Include(g => g.Owner).ToListAsync(); // Example include
        }
        */

        public override async Task AddAsync(Group entity, CancellationToken cancellationToken = default) // Added override
        {
            await _dbSet.AddAsync(entity, cancellationToken); // Use _dbSet from base
        }

        public override void Update(Group group) // Added override
        {
            _dbSet.Update(group); // Use _dbSet from base
        }

        public override void Remove(Group group) // Added override
        {
            _dbSet.Remove(group); // Use _dbSet from base
        }

        public async Task<bool> IsUserMemberOfGroupAsync(Guid userId, Guid groupId)
        {
            // 假设 GroupMember 实体有 UserId、GroupId，以及可能的 IsActive 或 HasLeft 标志。
            // 在此示例中，我们假设进行简单的成员资格检查。
            // 如果 GroupMember 有复合键，请确保 DbContext 配置正确。
            return await _context.GroupMembers
                                 .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId /* && !gm.HasLeft (如果适用) */);
        }

        public async Task<IEnumerable<Guid>> GetGroupIdsForUserAsync(Guid userId)
        {
            // 假设 GroupMember 实体中有一个 UserId 属性和一个 GroupId 属性。
            // 如果 GroupMember 有 HasLeft 或类似的状态字段，也应该在这里考虑。
            return await _context.GroupMembers
                                 .Where(gm => gm.UserId == userId /* && !gm.HasLeft (如果适用) */)
                                 .Select(gm => gm.GroupId)
                                 .Distinct() // 确保每个群组ID只返回一次
                                 .ToListAsync();
       }

       public async Task<Group?> GetByIdWithMembersAsync(Guid id)
       {
           return await _context.Groups
                                 .Include(g => g.Members) // 预加载成员
                                     // .ThenInclude(gm => gm.User) // 如果你需要成员的完整 User 详细信息
                                 .FirstOrDefaultAsync(g => g.Id == id);
      }

       public async Task<Group?> GetByNameAndOwnerAsync(string name, Guid ownerId)
       {
           return await _context.Groups
                                .FirstOrDefaultAsync(g => g.Name == name && g.OwnerId == ownerId);
       }

       public async Task AddGroupMemberAsync(GroupMember groupMember)
       {
           await _context.GroupMembers.AddAsync(groupMember);
       }

       public async Task<IEnumerable<Group>> GetUserGroupsAsync(Guid userId)
       {
           // 根据用户ID从GroupMembers表中筛选出该用户所在的群组ID
           // 然后根据这些群组ID从Groups表中获取群组信息
           return await _context.GroupMembers
                                .Where(gm => gm.UserId == userId)
                                .Select(gm => gm.Group) // 直接选择关联的Group实体
                                .Distinct() // 确保每个群组只返回一次
                                .ToListAsync();
       }

        // Implementation for IGenericRepository<Group>
        public override async Task<IEnumerable<Group>> FindAsync(Expression<Func<Group, bool>> predicate, CancellationToken cancellationToken = default) // Added CancellationToken and override
        {
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken); // Use _dbSet from base
        }

        // AddRangeAsync is inherited
        // UpdateRange is inherited
        // RemoveRange is inherited
        // Queryable is inherited
        // CountAsync is inherited
        // ExistsAsync is inherited
   }
}