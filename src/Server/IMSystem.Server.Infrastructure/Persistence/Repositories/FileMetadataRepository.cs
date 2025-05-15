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
/// 文件元数据仓储的EF Core实现。
/// </summary>
public class FileMetadataRepository : GenericRepository<FileMetadata>, IFileMetadataRepository
{
    // _context is inherited from GenericRepository

    public FileMetadataRepository(ApplicationDbContext context) : base(context) // Call base constructor
    {
        // _context = context ?? throw new ArgumentNullException(nameof(context)); // Handled by base
    }

    public override async Task AddAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default) // Added override
    {
        await _dbSet.AddAsync(fileMetadata, cancellationToken); // Use _dbSet from base
    }

    public override async Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet // Use _dbSet from base
            .Include(fm => fm.Uploader)
            .FirstOrDefaultAsync(fm => fm.Id == id, cancellationToken);
    }

    public override void Update(FileMetadata fileMetadata) // Added override
    {
        _dbSet.Update(fileMetadata); // Use _dbSet from base
    }

    public async Task<FileMetadata?> GetByStoredFileNameAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        return await _context.FileMetadatas
            .Include(fm => fm.Uploader)
            .FirstOrDefaultAsync(fm => fm.StoredFileName == storedFileName, cancellationToken);
    }

    public async Task<IEnumerable<FileMetadata>> GetByUploaderIdAsync(Guid uploaderId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.FileMetadatas
            .Where(fm => fm.CreatedBy == uploaderId)
            .OrderByDescending(fm => fm.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(fm => fm.Uploader)
            .ToListAsync(cancellationToken);
    }

    public override void Remove(FileMetadata fileMetadata) // Added override
    {
        _dbSet.Remove(fileMetadata); // Use _dbSet from base
    }

    public Task RemoveAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(fileMetadata);
        return Task.CompletedTask; // Or await _context.SaveChangesAsync(cancellationToken); if Remove should persist immediately
    }

    public async Task<IEnumerable<FileMetadata>> GetUnconfirmedUploadsOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        return await _context.FileMetadatas
            .Where(fm => !fm.IsConfirmed && fm.CreatedAt < olderThan)
            .ToListAsync(cancellationToken);
    }

    // Implementation for IGenericRepository<FileMetadata>
    public override async Task<IEnumerable<FileMetadata>> GetAllAsync(CancellationToken cancellationToken = default) // Updated signature
    {
        return await _dbSet.Include(fm => fm.Uploader).ToListAsync(cancellationToken); // Use _dbSet from base
    }

    public override async Task<IEnumerable<FileMetadata>> FindAsync(Expression<Func<FileMetadata, bool>> predicate, CancellationToken cancellationToken = default) // Added CancellationToken and override
    {
        return await _dbSet.Where(predicate).Include(fm => fm.Uploader).ToListAsync(cancellationToken); // Use _dbSet from base
    }

    // AddRangeAsync is inherited
    // UpdateRange is inherited
    // RemoveRange is inherited
    // Queryable is inherited
    // CountAsync is inherited
    // ExistsAsync is inherited
}