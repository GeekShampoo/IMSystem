using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Signaling;
using IMSystem.Protocol.DTOs.Notifications.Signaling;
using IMSystem.Protocol.Enums;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Signaling.EventHandlers
{
    /// <summary>
    /// 通话拒绝事件处理器
    /// </summary>
    public class CallRejectedEventHandler : INotificationHandler<CallRejectedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<CallRejectedEventHandler> _logger;

        public CallRejectedEventHandler(
            INotificationService notificationService,
            ILogger<CallRejectedEventHandler> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(CallRejectedEvent domainEvent, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("处理通话拒绝事件：CallId={CallId}, CallerId={CallerId}, CalleeId={CalleeId}, Reason={Reason}",
                    domainEvent.CallId, domainEvent.CallerId, domainEvent.CalleeId, domainEvent.Reason);

                var notification = new CallStateChangedNotificationDto
                {
                    CallId = domainEvent.CallId,
                    CallerId = domainEvent.CallerId,
                    CalleeId = domainEvent.CalleeId,
                    CallState = CallState.Rejected,
                    Reason = domainEvent.Reason,
                    Timestamp = domainEvent.Timestamp
                };

                // 通知主叫
                await _notificationService.SendNotificationAsync(
                    domainEvent.CallerId.ToString(),
                    "CallStateChanged",
                    notification);

                // 通知被叫(自己)
                await _notificationService.SendNotificationAsync(
                    domainEvent.CalleeId.ToString(),
                    "CallStateChanged",
                    notification);

                _logger.LogInformation("成功发送通话拒绝通知：CallId={CallId}, Reason={Reason}", 
                    domainEvent.CallId, domainEvent.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理通话拒绝事件时发生错误：CallId={CallId}", domainEvent.CallId);
                // 这里我们只记录异常，不重新抛出，以免中断其他事件处理
            }
        }
    }
}