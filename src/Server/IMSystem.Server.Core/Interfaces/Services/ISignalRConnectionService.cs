using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Services
{
    /// <summary>
    /// 定义SignalR连接管理服务的接口，用于添加/移除用户与群组等操作
    /// </summary>
    public interface ISignalRConnectionService
    {
        /// <summary>
        /// 将用户的所有活跃连接添加到指定的SignalR群组
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="groupId">群组ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>成功添加的连接数量</returns>
        Task<int> AddUserToSignalRGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将用户的所有活跃连接从指定的SignalR群组中移除
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="groupId">群组ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>成功移除的连接数量</returns>
        Task<int> RemoveUserFromSignalRGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的所有活跃连接ID列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>连接ID列表</returns>
        IEnumerable<string> GetUserConnections(string userId);
    }
}