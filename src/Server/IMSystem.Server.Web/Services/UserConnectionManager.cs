using IMSystem.Server.Web.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IMSystem.Server.Web.Services
{
    /// <summary>
    /// 实现用户与SignalR连接之间映射关系管理
    /// </summary>
    public class UserConnectionManager : IUserConnectionManager
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _userConnectionMap;
        private readonly ConcurrentDictionary<string, string> _connectionUserMap;
        private readonly ILogger<UserConnectionManager> _logger;

        public UserConnectionManager(ILogger<UserConnectionManager> logger)
        {
            _userConnectionMap = new ConcurrentDictionary<string, HashSet<string>>();
            _connectionUserMap = new ConcurrentDictionary<string, string>();
            _logger = logger;
        }

        /// <inheritdoc/>
        public void AddConnection(string userId, string connectionId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(connectionId))
            {
                _logger.LogWarning("尝试添加空的用户ID或连接ID");
                return;
            }

            // 将连接ID添加到用户的连接集合中
            _userConnectionMap.AddOrUpdate(
                userId,
                // 如果用户不存在，创建新的HashSet并添加连接ID
                _ => new HashSet<string> { connectionId },
                // 如果用户已存在，获取现有的HashSet并添加连接ID
                (_, connections) =>
                {
                    lock (connections)
                    {
                        connections.Add(connectionId);
                        return connections;
                    }
                });

            // 记录连接ID对应的用户ID
            _connectionUserMap[connectionId] = userId;

            _logger.LogDebug("用户 {UserId} 添加了连接 {ConnectionId}", userId, connectionId);
        }

        /// <inheritdoc/>
        public void RemoveConnection(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                _logger.LogWarning("尝试移除空的连接ID");
                return;
            }

            // 获取连接对应的用户ID
            if (_connectionUserMap.TryRemove(connectionId, out string userId))
            {
                // 从用户的连接集合中移除此连接ID
                if (_userConnectionMap.TryGetValue(userId, out var connections))
                {
                    lock (connections)
                    {
                        connections.Remove(connectionId);
                        
                        // 如果用户没有剩余连接，从字典中移除该用户
                        if (connections.Count == 0)
                        {
                            _userConnectionMap.TryRemove(userId, out _);
                            _logger.LogDebug("用户 {UserId} 没有剩余连接，已从连接管理器中移除", userId);
                        }
                    }
                }

                _logger.LogDebug("连接 {ConnectionId} 已从用户 {UserId} 的连接列表中移除", connectionId, userId);
            }
            else
            {
                _logger.LogWarning("尝试移除不存在的连接ID: {ConnectionId}", connectionId);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetUserConnections(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("尝试获取空用户ID的连接");
                return Enumerable.Empty<string>();
            }

            if (_userConnectionMap.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    // 返回连接ID的副本，避免在迭代时修改集合
                    return connections.ToList();
                }
            }

            return Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public string GetUserIdByConnection(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                _logger.LogWarning("尝试通过空连接ID获取用户ID");
                return null;
            }

            _connectionUserMap.TryGetValue(connectionId, out string userId);
            return userId;
        }
    }
}