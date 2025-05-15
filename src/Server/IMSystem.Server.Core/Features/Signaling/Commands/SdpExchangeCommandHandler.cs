using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Exceptions;

using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Signaling.Commands
{
    /// <summary>
    /// SDP 交换命令处理器
    /// </summary>
    public class SdpExchangeCommandHandler : IRequestHandler<SdpExchangeCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public SdpExchangeCommandHandler(
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

        public async Task<Result> Handle(SdpExchangeCommand request, CancellationToken cancellationToken)
        {
            // 1. 校验发送方和接收方用户是否存在
            var sender = await _userRepository.GetByIdAsync(request.SenderId, cancellationToken);
            var receiver = await _userRepository.GetByIdAsync(request.ReceiverId, cancellationToken);
            if (sender == null || receiver == null)
                throw new DomainException("发送方或接收方用户不存在");

            // 2. 业务校验（如通话ID合法性，可扩展）

            // 3. 通过领域实体添加领域事件（将由 ApplicationDbContext 的 DispatchDomainEventsAsync 统一处理）
            var sdpExchangedEvent = new IMSystem.Server.Domain.Events.Signaling.SdpExchangedEvent(
                request.CallId,
                request.SenderId,
                request.ReceiverId,
                request.Sdp,
                request.SdpType.ToString(),
                request.Timestamp
            );
            
            // 由于系统中可能不存在 CallSession 实体，我们直接使用 sender 作为事件发布者
            // 这确保事件能被记录并最终发布，同时通过 Outbox 模式保证了事件传递的可靠性
            sender.AddDomainEvent(sdpExchangedEvent);

            // 4. 删除直接推送代码，改由领域事件处理器负责通知

            // 5. 保存变更，触发领域事件处理
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}