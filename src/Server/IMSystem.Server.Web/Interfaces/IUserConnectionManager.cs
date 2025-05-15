using System.Collections.Generic;

namespace IMSystem.Server.Web.Interfaces
{
    /// <summary>
    /// 管理用户与SignalR连接之间的映射关系的接口
    /// </summary>
    public interface IUserConnectionManager
    {
        /// <summary>
        /// 添加用户的SignalR连接ID
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="connectionId">SignalR连接ID</param>
        void AddConnection(string userId, string connectionId);

        /// <summary>
        /// 移除用户的SignalR连接ID
        /// </summary>
        /// <param name="connectionId">SignalR连接ID</param>
        void RemoveConnection(string connectionId);

        /// <summary>
        /// 获取用户的所有SignalR连接ID
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>连接ID集合</returns>
        IEnumerable<string> GetUserConnections(string userId);

        /// <summary>
        /// 获取连接ID对应的用户ID
        /// </summary>
        /// <param name="connectionId">SignalR连接ID</param>
        /// <returns>用户ID，如果找不到则返回null</returns>
        string GetUserIdByConnection(string connectionId);
    }
}