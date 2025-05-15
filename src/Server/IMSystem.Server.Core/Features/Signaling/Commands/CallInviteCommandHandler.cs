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
    /// 发起通话邀请命令处理器
    /// </summary>
    public class CallInviteCommandHandler : IRequestHandler<CallInviteCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CallInviteCommandHandler> _logger;

        public CallInviteCommandHandler(
            IUserRepository userRepository,
            IMediator mediator,
            IUnitOfWork unitOfWork,
            ILogger<CallInviteCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(CallInviteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. 校验主叫和被叫用户是否存在
                var caller = await _userRepository.GetByIdAsync(request.CallerId, cancellationToken);
                var callee = await _userRepository.GetByIdAsync(request.CalleeId, cancellationToken);
                
                if (caller == null)
                {
                    _logger.LogWarning("发起通话邀请失败：主叫用户 {CallerId} 不存在", request.CallerId);
                    return Result.Failure(SignalingErrors.UserNotFound(request.CallerId));
                }
                
                if (callee == null)
                {
                    _logger.LogWarning("发起通话邀请失败：被叫用户 {CalleeId} 不存在", request.CalleeId);
                    return Result.Failure(SignalingErrors.UserNotFound(request.CalleeId));
                }

                // 2. 业务校验（如是否允许发起通话，可扩展）

                // 3. 生成通话ID
                var callId = Guid.NewGuid();

                // 4. 将领域事件添加到主叫用户实体（由 ApplicationDbContext 的 DispatchDomainEventsAsync 统一处理）
                var callInvitedEvent = new IMSystem.Server.Domain.Events.Signaling.CallInvitedEvent(
                    callId,
                    request.CallerId,
                    request.CalleeId,
                    request.CallType.ToString(),
                    request.Timestamp
                );
                
                // 由于系统中可能不存在 CallSession 实体，我们直接使用 caller 作为事件发布者
                caller.AddDomainEvent(callInvitedEvent);

                // 5. 删除直接推送代码，改由领域事件处理器负责通知
                
                // 6. 保存变更，触发领域事件处理
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("成功发起通话邀请：主叫用户 {CallerId} 邀请被叫用户 {CalleeId}，通话ID {CallId}，类型 {CallType}",
                    request.CallerId, request.CalleeId, callId, request.CallType);
                return Result.Success();
            }
            catch (DomainException dex)
            {
                _logger.LogWarning(dex, "处理通话邀请命令时发生领域异常：{ErrorMessage}", dex.Message);
                return Result.Failure(SignalingErrors.InviteError, dex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理通话邀请命令时发生意外错误");
                return Result.Failure(SignalingErrors.OperationFailed, "发起通话邀请时发生意外错误。");
            }
        }
    }
}