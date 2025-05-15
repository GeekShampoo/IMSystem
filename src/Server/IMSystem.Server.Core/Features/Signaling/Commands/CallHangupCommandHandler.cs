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

namespace IMSystem.Server.Core.Features.Signaling.Commands
{
    /// <summary>
    /// 通话挂断命令处理器
    /// </summary>
    public class CallHangupCommandHandler : IRequestHandler<CallHangupCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public CallHangupCommandHandler(
            IUserRepository userRepository,
            INotificationService notificationService,
            IMediator mediator,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _notificationService = notificationService;
            _mediator = mediator;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(CallHangupCommand request, CancellationToken cancellationToken)
        {
            // 1. 校验主叫和被叫用户是否存在
            var caller = await _userRepository.GetByIdAsync(request.CallerId, cancellationToken);
            var callee = await _userRepository.GetByIdAsync(request.CalleeId, cancellationToken);
            if (caller == null || callee == null)
                throw new DomainException("主叫或被叫用户不存在");

            // 2. 业务校验（如通话ID合法性，可扩展）

            // 3. 通过领域实体添加领域事件（将由 ApplicationDbContext 的 DispatchDomainEventsAsync 统一处理）
            var callHungupEvent = new IMSystem.Server.Domain.Events.Signaling.CallHungupEvent(
                request.CallId,
                request.CallerId,
                request.CalleeId,
                request.Reason,
                request.Timestamp
            );
            
            // 由于系统中可能不存在 CallSession 实体，我们直接使用 caller 作为事件发布者
            // 默认使用 caller 作为挂断发起方，后续可扩展 CallHangupCommand 添加 InitiatorId 参数
            caller.AddDomainEvent(callHungupEvent);

            // 4. 保存变更，触发领域事件处理
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 注意：移除了直接调用通知服务的代码，改由领域事件处理器负责通知

            return Result.Success();
        }
    }
}