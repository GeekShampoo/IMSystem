using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IMSystem.Protocol.DTOs.Responses.Groups; // Added for GroupDto and GroupMemberDto

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// 数据库服务接口
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// 初始化数据库，确保表已创建。
        /// </summary>
        Task InitializeDatabaseAsync();

        /// <summary>
        /// 确保数据库表结构存在，根据配置自动执行模式创建/验证
        /// </summary>
        /// <returns>异步任务</returns>
        Task EnsureDatabaseSchemaAsync();

        /// <summary>
        /// 执行SQL查询并返回结果集
        /// </summary>
        /// <typeparam name="T">返回的对象类型</typeparam>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="parameters">查询参数</param>
        /// <returns>查询结果集</returns>
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null);

        /// <summary>
        /// 执行SQL查询并返回第一个结果
        /// </summary>
        /// <typeparam name="T">返回的对象类型</typeparam>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="parameters">查询参数</param>
        /// <returns>查询的第一个结果，如果未找到则为 null</returns>
        Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null) where T : class; // Added class constraint for nullable T

        /// <summary>
        /// 执行非查询SQL语句并返回受影响的行数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数</param>
        /// <returns>受影响的行数</returns>
        Task<int> ExecuteAsync(string sql, object? parameters = null);

        /// <summary>
        /// 执行SQL并返回单一值
        /// </summary>
        /// <typeparam name="T">返回的值类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数</param>
        /// <returns>单一值</returns>
        Task<T> ExecuteScalarAsync<T>(string sql, object? parameters = null);

        /// <summary>
        /// 在事务中执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <returns>操作结果</returns>
        Task<bool> ExecuteInTransactionAsync(Func<Task> action);

        // GroupDto CRUD Operations
        Task SaveGroupAsync(GroupDto group);
        Task<GroupDto?> GetGroupAsync(Guid groupId); // Return nullable GroupDto
        Task<List<GroupDto>> GetUserGroupsAsync(); // 保留原有方法用于获取所有群组
        Task<List<GroupDto>> GetUserJoinedGroupsAsync(Guid userId); // 新增方法，用于获取特定用户加入的群组
        Task DeleteGroupAsync(Guid groupId);
        Task SaveGroupsAsync(IEnumerable<GroupDto> groups);

        // GroupMemberDto CRUD Operations
        Task SaveGroupMemberAsync(Guid groupId, GroupMemberDto member);
        Task SaveGroupMembersAsync(Guid groupId, IEnumerable<GroupMemberDto> members);
        Task<List<GroupMemberDto>> GetGroupMembersAsync(Guid groupId);
        Task DeleteGroupMemberAsync(Guid groupId, Guid userId); // Changed userId to Guid
        Task DeleteGroupMembersAsync(Guid groupId);
    }
}