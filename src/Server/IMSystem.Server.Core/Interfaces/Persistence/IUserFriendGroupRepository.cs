using IMSystem.Server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence;

/// <summary>
/// 定义用户好友与分组关联（<see cref="UserFriendGroup"/>）仓储的接口。
/// </summary>
public interface IUserFriendGroupRepository : IGenericRepository<UserFriendGroup>
{
    // AddAsync, Remove, GetByIdAsync, Queryable, etc.
    // are inherited from IGenericRepository<UserFriendGroup>.
    
    /// <summary>
    /// 根据用户ID和好友关系ID异步获取用户好友与分组的关联。
    /// 由于一个好友在一个用户下只能属于一个分组 (基于索引 CreatedBy, FriendshipId)，此方法用于查找该好友当前所在的分组。
    /// </summary>
    /// <param name="userId">执行分组操作的用户ID (即分组的拥有者)。</param>
    /// <param name="friendshipId">好友关系ID。</param>
    /// <returns>找到的 <see cref="UserFriendGroup"/> 实体；如果好友未分配到任何分组，则返回 null。</returns>
    Task<UserFriendGroup?> GetByUserIdAndFriendshipIdAsync(Guid userId, Guid friendshipId);

    /// <summary>
    /// 根据好友分组ID和好友关系ID异步获取用户好友与分组的关联。
    /// 用于检查特定好友是否已在特定分组中。
    /// </summary>
    /// <param name="friendGroupId">好友分组ID。</param>
    /// <param name="friendshipId">好友关系ID。</param>
    /// <returns>找到的 <see cref="UserFriendGroup"/> 实体；如果未找到，则返回 null。</returns>
    Task<UserFriendGroup?> GetByFriendGroupIdAndFriendshipIdAsync(Guid friendGroupId, Guid friendshipId);

    /// <summary>
    /// 获取指定好友分组下的所有好友关联记录。
    /// </summary>
    /// <param name="friendGroupId">好友分组ID。</param>
    /// <returns>该分组下的 <see cref="UserFriendGroup"/> 实体集合。</returns>
    Task<IEnumerable<UserFriendGroup>> GetByFriendGroupIdAsync(Guid friendGroupId);

    /// <summary>
    /// 异步移除所有与指定好友关系ID相关联的用户好友与分组的关联记录。
    /// </summary>
    /// <param name="friendshipId">要移除关联记录的好友关系ID。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task RemoveByFriendshipIdAsync(Guid friendshipId, CancellationToken cancellationToken = default);
}