using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Enums;
using IMSystem.Server.Core.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace IMSystem.Server.Infrastructure.Persistence.Repositories;

public class GroupInvitationRepository : GenericRepository<GroupInvitation>, IGroupInvitationRepository
{
    // _context is inherited from GenericRepository

    public GroupInvitationRepository(ApplicationDbContext context) : base(context) // Call base constructor
    {
        // _context = context ?? throw new ArgumentNullException(nameof(context)); // Handled by base
    }

    // IGroupInvitationRepository specific methods
    public async Task<GroupInvitation?> FindByGroupAndInvitedUserAsync(Guid groupId, Guid invitedUserId)
    {
        return await _context.GroupInvitations
            .FirstOrDefaultAsync(gi => gi.GroupId == groupId && gi.InvitedUserId == invitedUserId && gi.Status == GroupInvitationStatus.Pending);
    }

    public async Task<IEnumerable<GroupInvitation>> GetPendingInvitationsForUserAsync(Guid userId)
    {
        return await _context.GroupInvitations
            .Include(gi => gi.Group)
            .Include(gi => gi.Inviter)
            .Include(gi => gi.InvitedUser) // Added to load InvitedUser details for DTO mapping
            .Where(gi => gi.InvitedUserId == userId && gi.Status == GroupInvitationStatus.Pending)
            .OrderByDescending(gi => gi.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<GroupInvitation>> GetInvitationsSentByUserAsync(Guid inviterId)
    {
        return await _context.GroupInvitations
            .Include(gi => gi.Group)
            .Include(gi => gi.InvitedUser)
            .Where(gi => gi.InviterId == inviterId)
            .OrderByDescending(gi => gi.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<GroupInvitation>> GetInvitationsSentByGroupAsync(Guid groupId)
    {
        return await _context.GroupInvitations
            .Include(gi => gi.Group) // Though groupId is known, including Group might be useful for consistency or if GroupName is needed directly from Group object
            .Include(gi => gi.Inviter)
            .Include(gi => gi.InvitedUser)
            .Where(gi => gi.GroupId == groupId)
            .OrderByDescending(gi => gi.CreatedAt)
            .ToListAsync();
    }

    // Implementation of IGenericRepository<GroupInvitation>
    public override async Task<GroupInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet // Use _dbSet from base
            .Include(gi => gi.Group)
            .Include(gi => gi.Inviter)
            .Include(gi => gi.InvitedUser)
            .FirstOrDefaultAsync(gi => gi.Id == id, cancellationToken);
    }

    // GetAllAsync is inherited from GenericRepository. Override if includes are always needed.
    /*
    public override async Task<IEnumerable<GroupInvitation>> GetAllAsync()
    {
        return await _dbSet.Include(gi => gi.Group).Include(gi => gi.Inviter).Include(gi => gi.InvitedUser).ToListAsync();
    }
    */

    public override async Task<IEnumerable<GroupInvitation>> FindAsync(Expression<Func<GroupInvitation, bool>> predicate, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet.Where(predicate) // Use _dbSet from base
            .Include(gi => gi.Group)        // Optional: Add includes if commonly needed for FindAsync scenarios
            .Include(gi => gi.Inviter)
            .Include(gi => gi.InvitedUser)
            .ToListAsync(cancellationToken);
    }

    public override async Task AddAsync(GroupInvitation entity, CancellationToken cancellationToken = default) // Added override
    {
        await _dbSet.AddAsync(entity, cancellationToken); // Use _dbSet from base
    }

    // AddRangeAsync is inherited

    public override void Update(GroupInvitation entity) // Added override
    {
        _dbSet.Update(entity); // Use _dbSet from base
    }

    // UpdateRange is inherited

    public override void Remove(GroupInvitation entity) // Added override
    {
        _dbSet.Remove(entity); // Use _dbSet from base
    }

    // RemoveRange is inherited
    // Queryable is inherited
    // CountAsync is inherited
    // ExistsAsync is inherited
}