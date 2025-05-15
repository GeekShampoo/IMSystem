using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Exceptions;
using IMSystem.Protocol.DTOs.Notifications.Signaling;
using IMSystem.Protocol.Enums;
using IMSystem.Protocol.Common;
using Microsoft.Extensions.Logging;
using IMSystem.Server.Core.Common;

namespace IMSystem.Server.Core.Features.Signaling.Commands
{
    /// <summary>
    /// 通话拒绝命令处理器
    /// </summary>
    public class CallRejectCommandHandler : IRequestHandler<CallRejectCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CallRejectCommandHandler> _logger;

        public CallRejectCommandHandler(
            IUserRepository userRepository,
            INotificationService notificationService,
            IMediator mediator,
            IUnitOfWork unitOfWork,
            ILogger<CallRejectCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(CallRejectCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. 校验主叫和被叫用户是否存在
                var caller = await _userRepository.GetByIdAsync(request.CallerId, cancellationToken);
                var callee = await _userRepository.GetByIdAsync(request.CalleeId, cancellationToken);
                
                if (caller == null)
                {
                    _logger.LogWarning("通话拒绝失败：主叫用户 {CallerId} 不存在", request.CallerId);
                    return Result.Failure(SignalingErrors.UserNotFound(request.CallerId));
                }
                
                if (callee == null)
                {
                    _logger.LogWarning("通话拒绝失败：被叫用户 {CalleeId} 不存在", request.CalleeId);
                    return Result.Failure(SignalingErrors.UserNotFound(request.CalleeId));
                }

                // 2. 业务校验（如通话ID合法性，可扩展）

                // 3. 通过领域实体添加领域事件（将由 ApplicationDbContext 的 DispatchDomainEventsAsync 统一处理）
                var callRejectedEvent = new IMSystem.Server.Domain.Events.Signaling.CallRejectedEvent(
                    request.CallId,
                    request.CallerId,
                    request.CalleeId,
                    request.Reason,
                    request.Timestamp
                );
                
                // 由于系统中可能不存在 CallSession 实体，我们直接使用 callee (被叫) 作为事件发布者
                callee.AddDomainEvent(callRejectedEvent);

                // 4. 保存变更，触发领域事件处理
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 注意：移除了直接调用通知服务的代码，改由领域事件处理器负责通知
                
                _logger.LogInformation("通话拒绝成功：被叫用户 {CalleeId} 拒绝了主叫用户 {CallerId} 的通话，通话ID {CallId}，原因 {Reason}",
                    request.CalleeId, request.CallerId, request.CallId, request.Reason);
                return Result.Success();
            }
            catch (DomainException dex)
            {
                _logger.LogWarning(dex, "处理通话拒绝命令时发生领域异常：{ErrorMessage}", dex.Message);
                return Result.Failure(SignalingErrors.OperationFailed, dex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理通话拒绝命令时发生意外错误");
                return Result.Failure(SignalingErrors.OperationFailed, "拒绝通话时发生意外错误。");
            }
        }
    }
}