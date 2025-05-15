using IMSystem.Server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence;

/// <summary>
/// 定义好友分组仓储的接口，用于执行与 <see cref="FriendGroup"/> 实体相关的数据库操作。
/// </summary>
public interface IFriendGroupRepository : IGenericRepository<FriendGroup>
{
    // AddAsync, GetByIdAsync, Update, Remove are inherited from IGenericRepository<FriendGroup>.
    // GetAllAsync, FindAsync, AddRangeAsync, UpdateRange, RemoveRange, Queryable, CountAsync, ExistsAsync(predicate)
    // are also inherited.

    /// <summary>
    /// 异步获取指定用户的所有好友分组。
    /// </summary>
    /// <param name="userId">用户ID。</param>
    /// <returns>该用户的 <see cref="FriendGroup"/> 实体集合。</returns>
    Task<IEnumerable<FriendGroup>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// 异步获取指定用户和分组名称的好友分组。
    /// </summary>
    /// <param name="name">分组名称。</param>
    /// <param name="userId">用户ID。</param>
    /// <returns>如果存在，则返回 <see cref="FriendGroup"/> 实体；否则返回 null。</returns>
    Task<FriendGroup?> GetByNameAndUserIdAsync(string name, Guid userId);
    
    /// <summary>
    /// 异步获取指定用户的默认好友分组。
    /// </summary>
    /// <param name="userId">用户ID。</param>
    /// <returns>如果存在，则返回用户的默认 <see cref="FriendGroup"/> 实体；否则返回 null。</returns>
    Task<FriendGroup?> GetDefaultByUserIdAsync(Guid userId);

    /// <summary>
    /// Checks if a friend group with the specified order already exists for the user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="order">The order value to check.</param>
    /// <returns>True if a group with the specified order exists for the user, false otherwise.</returns>
    Task<bool> ExistsByUserIdAndOrderAsync(Guid userId, int order);

    /// <summary>
    /// Gets a friend group by user ID and order, excluding a specific group ID.
    /// Used to check if an order is already taken by another group of the same user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="order">The order value.</param>
    /// <param name="excludeGroupId">The group ID to exclude from the search.</param>
    /// <returns>The friend group if found; otherwise, null.</returns>
    Task<FriendGroup?> GetByUserIdAndOrderExcludingGroupIdAsync(Guid userId, int order, Guid excludeGroupId);

    // 未来可能需要的方法：
    // Task<bool> ExistsAsync(Guid groupId); // This specific overload might be redundant
    // Task<int> GetMaxOrderAsync(Guid userId); // 用于确定新分组的默认 Order
}