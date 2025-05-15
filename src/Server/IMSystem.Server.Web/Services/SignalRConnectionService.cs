using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Web.Hubs;
using IMSystem.Server.Web.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Web.Services
{
    /// <summary>
    /// 实现ISignalRConnectionService接口，提供SignalR连接和群组管理功能
    /// </summary>
    public class SignalRConnectionService : ISignalRConnectionService
    {
        private readonly IHubContext<MessagingHub> _messagingHubContext;
        private readonly IUserConnectionManager _userConnectionManager;
        private readonly ILogger<SignalRConnectionService> _logger;

        public SignalRConnectionService(
            IHubContext<MessagingHub> messagingHubContext,
            IUserConnectionManager userConnectionManager,
            ILogger<SignalRConnectionService> logger)
        {
            _messagingHubContext = messagingHubContext ?? throw new ArgumentNullException(nameof(messagingHubContext));
            _userConnectionManager = userConnectionManager ?? throw new ArgumentNullException(nameof(userConnectionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<int> AddUserToSignalRGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
        {
            var userConnections = GetUserConnections(userId.ToString());
            int successCount = 0;

            if (userConnections != null && userConnections.Any())
            {
                string groupIdString = groupId.ToString();
                
                foreach (var connectionId in userConnections)
                {
                    try
                    {
                        await _messagingHubContext.Groups.AddToGroupAsync(connectionId, groupIdString, cancellationToken);
                        successCount++;
                        _logger.LogDebug("用户 {UserId} 的连接 {ConnectionId} 已加入SignalR群组 {GroupId}", 
                            userId, connectionId, groupId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "将连接 {ConnectionId} 添加到群组 {GroupId} 失败", connectionId, groupId);
                    }
                }
            }

            return successCount;
        }

        /// <inheritdoc/>
        public async Task<int> RemoveUserFromSignalRGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
        {
            var userConnections = GetUserConnections(userId.ToString());
            int successCount = 0;

            if (userConnections != null && userConnections.Any())
            {
                string groupIdString = groupId.ToString();
                
                foreach (var connectionId in userConnections)
                {
                    try
                    {
                        await _messagingHubContext.Groups.RemoveFromGroupAsync(connectionId, groupIdString, cancellationToken);
                        successCount++;
                        _logger.LogDebug("用户 {UserId} 的连接 {ConnectionId} 已从SignalR群组 {GroupId} 移除", 
                            userId, connectionId, groupId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "将连接 {ConnectionId} 从群组 {GroupId} 移除失败", connectionId, groupId);
                    }
                }
            }

            return successCount;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetUserConnections(string userId)
        {
            return _userConnectionManager.GetUserConnections(userId);
        }
    }
}