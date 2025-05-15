using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Enums; // Added for FriendshipStatus
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence;

/// <summary>
/// 定义好友关系仓储的接口，用于执行与 <see cref="Friendship"/> 实体相关的数据库操作。
/// </summary>
public interface IFriendshipRepository : IGenericRepository<Friendship>
{
    // AddAsync, GetByIdAsync, FindAsync, Update, Remove, Queryable, etc.
    // are inherited from IGenericRepository<Friendship>.

    /// <summary>
    /// 异步获取两个特定用户之间的好友关系（不区分请求者和接收者）。
    /// </summary>
    /// <param name="userId1">第一个用户的ID。</param>
    /// <param name="userId2">第二个用户的ID。</param>
    /// <returns>如果存在，则返回 <see cref="Friendship"/> 实体；否则返回 null。</returns>
    /// <remarks>此方法应能处理 (userId1, userId2) 和 (userId2, userId1) 的情况。</remarks>
    Task<Friendship?> GetFriendshipBetweenUsersAsync(Guid userId1, Guid userId2);
    
    /// <summary>
    /// 异步获取两个特定用户之间的好友关系（不区分请求者和接收者）。
    /// </summary>
    /// <param name="userId1">第一个用户的ID。</param>
    /// <param name="userId2">第二个用户的ID。</param>
    /// <returns>如果存在，则返回 <see cref="Friendship"/> 实体；否则返回 null。</returns>
    /// <remarks>此方法与 GetFriendshipBetweenUsersAsync 功能相同，提供更简洁的命名方式。</remarks>
    Task<Friendship?> GetFriendshipAsync(Guid userId1, Guid userId2);
    
    /// <summary>
    /// 异步获取用户的所有好友关系（例如，状态为 Accepted 的）。
    /// </summary>
    /// <param name="userId">用户ID。</param>
    /// <param name="status">可选的好友关系状态进行过滤。</param>
    /// <returns>满足条件的 <see cref="Friendship"/> 实体集合。</returns>
    Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(Guid userId, FriendshipStatus? status = null);

    /// <summary>
    /// 异步获取用户的所有好友关系（支持分页）。
    /// </summary>
    /// <param name="userId">用户ID。</param>
    /// <param name="status">可选的好友关系状态进行过滤。</param>
    /// <param name="pageNumber">页码（从1开始）。</param>
    /// <param name="pageSize">每页数量。</param>
    /// <returns>满足条件的 <see cref="Friendship"/> 实体集合。</returns>
    Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(Guid userId, FriendshipStatus? status, int? pageNumber, int? pageSize);

    /// <summary>
    /// 异步获取用户收到的好友请求（状态为 Pending，且用户是 Addressee）。
    /// </summary>
    /// <param name="userId">接收请求的用户ID。</param>
    /// <returns>待处理的好友请求集合。</returns>
    Task<IEnumerable<Friendship>> GetPendingFriendRequestsForUserAsync(Guid userId);

    // FindAsync is inherited.
    // Update is inherited.
    // Remove is inherited.
}