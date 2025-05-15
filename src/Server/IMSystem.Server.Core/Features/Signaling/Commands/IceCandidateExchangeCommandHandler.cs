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
    /// ICE 候选交换命令处理器
    /// </summary>
    public class IceCandidateExchangeCommandHandler : IRequestHandler<IceCandidateExchangeCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public IceCandidateExchangeCommandHandler(
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

        public async Task<Result> Handle(IceCandidateExchangeCommand request, CancellationToken cancellationToken)
        {
            // 1. 校验发送方和接收方用户是否存在
            var sender = await _userRepository.GetByIdAsync(request.SenderId, cancellationToken);
            var receiver = await _userRepository.GetByIdAsync(request.ReceiverId, cancellationToken);
            if (sender == null || receiver == null)
                throw new DomainException("发送方或接收方用户不存在");

            // 2. 业务校验（如通话ID合法性，可扩展）

            // 3. 将领域事件添加到发送方实体（由 ApplicationDbContext 的 DispatchDomainEventsAsync 统一处理）
            var iceCandidateExchangedEvent = new IMSystem.Server.Domain.Events.Signaling.IceCandidateExchangedEvent(
                request.CallId,
                request.SenderId,
                request.ReceiverId,
                request.Candidate,
                request.SdpMid,
                request.SdpMLineIndex,
                request.Timestamp
            );
            
            // 由于系统中可能不存在 CallSession 实体，我们直接使用 sender 作为事件发布者
            // 这确保事件能被记录并最终发布，同时通过 Outbox 模式保证了事件传递的可靠性
            sender.AddDomainEvent(iceCandidateExchangedEvent);

            // 4. 推送ICE信息给接收方
            var payload = new
            {
                CallId = request.CallId,
                SenderId = request.SenderId,
                Candidate = request.Candidate,
                SdpMid = request.SdpMid,
                SdpMLineIndex = request.SdpMLineIndex,
                Timestamp = request.Timestamp
            };
            await _notificationService.SendNotificationAsync(
                request.ReceiverId.ToString(),
                "IceCandidateExchanged",
                payload
            );
            
            // 5. 保存变更，触发领域事件处理
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}