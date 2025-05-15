using IMSystem.Server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence;

/// <summary>
/// 定义文件元数据仓储的接口。
/// </summary>
public interface IFileMetadataRepository : IGenericRepository<FileMetadata>
{
    // AddAsync(FileMetadata fileMetadata, CancellationToken cancellationToken) is inherited.
    // GetByIdAsync(Guid id, CancellationToken cancellationToken) is inherited.
    // Update(FileMetadata fileMetadata) is inherited.
    // Remove(FileMetadata fileMetadata) is inherited.

    /// <summary>
    /// 异步删除文件元数据。
    /// </summary>
    /// <param name="fileMetadata">要删除的文件元数据实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task RemoveAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步根据存储文件名获取文件元数据。
    /// </summary>
    /// <param name="storedFileName">文件在存储系统中的名称或唯一键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到的 <see cref="FileMetadata"/> 实体；如果未找到，则返回 null。</returns>
    Task<FileMetadata?> GetByStoredFileNameAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取指定用户上传的所有文件元数据（支持分页）。
    /// </summary>
    /// <param name="uploaderId">上传者用户ID。</param>
    /// <param name="pageNumber">页码。</param>
    /// <param name="pageSize">每页大小。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>文件元数据列表。</returns>
    Task<IEnumerable<FileMetadata>> GetByUploaderIdAsync(Guid uploaderId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取在指定时间之前创建且尚未确认的未完成文件上传记录。
    /// 用于清理过期的、未完成的上传。
    /// </summary>
    /// <param name="olderThan">用于比较的UTC时间戳。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>未确认的文件元数据列表。</returns>
    Task<IEnumerable<FileMetadata>> GetUnconfirmedUploadsOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}