using IMSystem.Server.Domain.Entities; // For GroupMember
namespace IMSystem.Server.Core.Interfaces.Persistence;

/// <summary>
/// 定义群组成员仓储的接口。
/// </summary>
public interface IGroupMemberRepository : IGenericRepository<GroupMember>
{
    /// <summary>
    /// 根据群组ID和用户ID获取群组成员关系。
    /// </summary>
    /// <param name="groupId">群组ID。</param>
    /// <param name="userId">用户ID。</param>
    /// <returns>如果找到，则返回 <see cref="GroupMember"/> 实体；否则返回 null。</returns>
    Task<GroupMember?> GetMemberOrDefaultAsync(Guid groupId, Guid userId);

    /// <summary>
    /// 根据群组ID和用户ID获取群组成员关系。
    /// </summary>
    /// <param name="groupId">群组ID。</param>
    /// <param name="userId">用户ID。</param>
    /// <returns>如果找到，则返回 <see cref="GroupMember"/>
    /// 实体；否则返回 null。</returns>
    /// <remarks>此方法与 GetMemberOrDefaultAsync 功能相同，提供更简洁的命名方式。</remarks>
    Task<GroupMember?> GetMembershipAsync(Guid groupId, Guid userId);

    /// <summary>
    /// 根据群组ID获取所有群组成员。
    /// </summary>
    /// <param name="groupId">群组ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>群组成员列表。</returns>
    Task<IEnumerable<GroupMember>> GetMembersByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a paged list of members for a specific group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the list of group members for the page and the total count of members.</returns>
    Task<(IEnumerable<GroupMember> Members, int TotalCount)> GetMembersByGroupIdPagedAsync(
        Guid groupId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // 根据需要可以添加其他与 GroupMember 相关的特定方法
    // 例如：
    // Task<IEnumerable<GroupMember>> GetMembersByUserIdAsync(Guid userId);
    // Task RemoveMemberAsync(Guid groupId, Guid userId);

    /// <summary>
    /// Checks if a user is a member of a specific group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user is a member of the group, false otherwise.</returns>
    Task<bool> IsUserMemberOfGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户所在的所有群组
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户所在的群组列表</returns>
    Task<IList<Group>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取群组的所有成员
    /// </summary>
    /// <param name="groupId">群组ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>群组成员列表</returns>
    Task<IList<GroupMember>> GetGroupMembersAsync(Guid groupId, CancellationToken cancellationToken = default);
}