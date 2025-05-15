using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using IMSystem.Server.Core.Interfaces.Persistence; // For IGroupRepository
using MediatR; // For IMediator
using IMSystem.Server.Core.Features.User.Commands; // For UpdateUserPresenceCommand
using IMSystem.Server.Core.Features.Group.Queries; // For IsUserMemberOfGroupQuery
using IMSystem.Protocol.Common; // For Result
using IMSystem.Protocol.DTOs.Requests.User;

namespace IMSystem.Server.Web.Hubs
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly ILogger<PresenceHub> _logger;
        private readonly IGroupRepository _groupRepository;
        private readonly IMediator _mediator;

        public PresenceHub(ILogger<PresenceHub> logger, IGroupRepository groupRepository, IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        /// <summary>
        /// 用户上线事件，刷新在线状态、记录LastSeenAt并通知好友
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userIdString = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userGuid))
            {
                // 刷新在线状态（上线）
                var presenceUpdateCommand = new UpdateUserPresenceCommand(userGuid, true);
                await _mediator.Send(presenceUpdateCommand);
                // LastSeenAt记录与好友通知由Core层领域事件机制处理
            }
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 用户下线事件，刷新在线状态、记录LastSeenAt并通知好友
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userIdString = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userGuid))
            {
                // 刷新在线状态（下线）
                var presenceUpdateCommand = new UpdateUserPresenceCommand(userGuid, false);
                await _mediator.Send(presenceUpdateCommand);
                // LastSeenAt记录与好友通知由Core层领域事件机制处理
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 客户端心跳，刷新在线状态与时间戳
        /// </summary>
        public async Task Heartbeat(HeartbeatRequestDto request)
        {
            var userIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userGuid))
            {
                _logger.LogWarning("Heartbeat called by unauthenticated or invalid user identifier.");
                return;
            }
            // 刷新在线状态
            var presenceUpdateCommand = new UpdateUserPresenceCommand(userGuid, true);
            await _mediator.Send(presenceUpdateCommand);
            // 可扩展：记录最后心跳时间，后台定时检测超时用户自动下线
        }
    }
}