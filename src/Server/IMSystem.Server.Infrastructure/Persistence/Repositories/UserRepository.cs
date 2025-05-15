using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace IMSystem.Server.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 用户仓储的实现。
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 初始化 <see cref="UserRepository"/> 类的新实例。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        public UserRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // 保证加载 Profile
            return await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Profile)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Profile)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddRangeAsync(IEnumerable<User> users, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddRangeAsync(users, cancellationToken);
        }

        /// <inheritdoc/>
        public void Update(User user)
        {
            // EF Core的DbContext会自动跟踪从数据库检索到的实体的更改。
            // 调用 _context.Users.Update(user) 通常用于附加一个分离的实体并将其标记为已修改。
            // 如果实体已由上下文跟踪，则此显式调用不是必需的，但这样做也无害。
            _context.Users.Update(user);
        }

        /// <inheritdoc/>
        public void Remove(User user)
        {
            _context.Users.Remove(user);
        }

        /// <inheritdoc/>
        public void RemoveRange(IEnumerable<User> users)
        {
            _context.Users.RemoveRange(users);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            // 需要处理 Email 可能为 null 的情况，如果数据库允许 Email 为 null
            return await _context.Users.AnyAsync(u => u.Email == email && u.Email != null);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetUsersByExternalIdsAsync(IEnumerable<Guid> externalIds)
        {
            if (externalIds == null || !externalIds.Any())
            {
                return Enumerable.Empty<User>();
            }
            return await _context.Users
                .Include(u => u.Profile)
                .Where(u => externalIds.Contains(u.Id))
                .ToListAsync();
        }

        /// <inheritdoc/>
        public IQueryable<User> Queryable() // Removed override
        {
            return _context.Users.AsQueryable();
        }

        /// <inheritdoc/>
        public async Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
            {
                return await _context.Users.CountAsync(cancellationToken);
            }
            return await _context.Users.CountAsync(predicate, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(predicate, cancellationToken);
        }
        
        // UpdateRange is missing from IGenericRepository implementation
        /// <inheritdoc/>
        public void UpdateRange(IEnumerable<User> users)
        {
            _context.Users.UpdateRange(users);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByIdWithProfileAsync(Guid id)
        {
            return await _context.Users
                                 .Include(u => u.Profile)
                                 .FirstOrDefaultAsync(u => u.Id == id);
       }
 
       /// <inheritdoc/>
       public async Task<IEnumerable<User>> GetUsersByExternalIdsWithProfileAsync(IEnumerable<Guid> externalIds)
       {
           if (externalIds == null || !externalIds.Any())
           {
               return Enumerable.Empty<User>();
           }
           return await _context.Users
                                .Where(u => externalIds.Contains(u.Id))
                                .Include(u => u.Profile)
                                .ToListAsync();
       }

       /// <inheritdoc/>
       public async Task<User?> FindByEmailVerificationTokenAsync(string token)
       {
           if (string.IsNullOrWhiteSpace(token))
           {
               return null;
           }
           // Find user by token and ensure the token has not expired.
           return await _context.Users
                                .Include(u => u.Profile) // Include profile if needed after verification
                                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token && u.EmailVerificationTokenExpiresAt > DateTimeOffset.UtcNow);
       }

       /// <inheritdoc/>
       public async Task<User?> GetByUsernameWithProfileAsync(string username)
       {
           return await _context.Users
                                .Include(u => u.Profile)
                                .FirstOrDefaultAsync(u => u.Username == username);
       }

       /// <inheritdoc/>
       public async Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
       {
           if (userIds == null || !userIds.Any())
           {
               return Enumerable.Empty<User>();
           }
           return await _context.Users
                                .Where(u => userIds.Contains(u.Id))
                                .Include(u => u.Profile) // Optionally include profile or other related data
                                .ToListAsync(cancellationToken);
       }
   }
}