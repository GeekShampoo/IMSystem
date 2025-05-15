using IMSystem.Server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        // GetByIdAsync, GetAllAsync, AddAsync, Update, Remove, Queryable, etc.
        // are inherited from IGenericRepository<Group>.

        /// <summary>
        /// 检查指定用户是否是指定群组的成员。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <param name="groupId">群组ID。</param>
        /// <returns>如果用户是群组成员，则为 true；否则为 false。</returns>
        Task<bool> IsUserMemberOfGroupAsync(Guid userId, Guid groupId);

        /// <summary>
        /// 获取指定用户所属的所有群组的ID列表。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>用户所属群组的ID列表。</returns>
        Task<IEnumerable<Guid>> GetGroupIdsForUserAsync(Guid userId);

        /// <summary>
        /// 根据ID异步获取群组及其成员信息。
        /// </summary>
        /// <param name="id">群组的唯一标识符。</param>
        /// <returns>表示异步操作的结果，包含找到的 <see cref="Group"/> 实体（包含成员）；如果未找到，则返回 null。</returns>
        Task<Group?> GetByIdWithMembersAsync(Guid id);

        /// <summary>
        /// 根据群组名称和群主ID异步获取群组。
        /// </summary>
        /// <param name="name">群组名称。</param>
        /// <param name="ownerId">群主的用户ID。</param>
        /// <returns>如果找到匹配的群组，则返回 <see cref="Group"/> 实体；否则返回 null。</returns>
        Task<Group?> GetByNameAndOwnerAsync(string name, Guid ownerId);

        /// <summary>
        /// 异步添加一个新的群组成员。
        /// </summary>
        /// <param name="groupMember">要添加的 <see cref="GroupMember"/> 实体。</param>
        Task AddGroupMemberAsync(GroupMember groupMember);

        /// <summary>
        /// 异步获取指定用户加入的所有群组。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>用户加入的群组列表。</returns>
        Task<IEnumerable<Group>> GetUserGroupsAsync(Guid userId);
    }
}