using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Infrastructure.Persistence.Repositories;

public class UserProfileRepository : GenericRepository<UserProfile>, IUserProfileRepository
{
    public UserProfileRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
    }
    
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet.FindAsync(new object[] { userId });
    }

    public override async Task<IEnumerable<UserProfile>> FindAsync(Expression<Func<UserProfile, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public override async Task AddAsync(UserProfile entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public override void Update(UserProfile entity)
    {
        _dbSet.Update(entity);
    }

    public override void Remove(UserProfile entity)
    {
        _dbSet.Remove(entity);
    }
}