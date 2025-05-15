using System.Threading.Tasks;
using IMSystem.Protocol.DTOs.Requests.Signaling;
using IMSystem.Protocol.DTOs.Notifications.Signaling;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using IMSystem.Server.Core.Features.Signaling.Commands;

namespace IMSystem.Server.Web.Hubs
{
    /// <summary>
    /// 音视频通话信令 Hub
    /// </summary>
    [Authorize]
    public class SignalingHub : Hub
    {
        private readonly IMediator _mediator;

        public SignalingHub(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// 发起通话邀请
        /// </summary>
        public async Task CallInvite(CallInviteRequest request)
        {
            var command = new CallInviteCommand(
                request.CallerId,
                request.CalleeId,
                request.CallType,
                request.Timestamp
            );
            await _mediator.Send(command);
        }

        /// <summary>
        /// 通话应答
        /// </summary>
        public async Task CallAnswer(CallAnswerRequest request)
        {
            var command = new CallAnswerCommand(
                request.CallerId,
                request.CalleeId,
                request.CallId,
                request.Accepted,
                request.Timestamp
            );
            await _mediator.Send(command);
        }

        /// <summary>
        /// 通话拒绝
        /// </summary>
        public async Task CallReject(CallRejectRequest request)
        {
            var command = new CallRejectCommand(
                request.CallerId,
                request.CalleeId,
                request.CallId,
                request.Reason,
                request.Timestamp
            );
            await _mediator.Send(command);
        }

        /// <summary>
        /// 通话挂断
        /// </summary>
        public async Task CallHangup(CallHangupRequest request)
        {
            var command = new CallHangupCommand(
                request.CallerId,
                request.CalleeId,
                request.CallId,
                request.Reason,
                request.Timestamp
            );
            await _mediator.Send(command);
        }

        /// <summary>
        /// SDP 交换
        /// </summary>
        public async Task SdpExchange(SdpExchangeRequest request)
        {
            var command = new SdpExchangeCommand(
                request.CallId,
                request.SenderId,
                request.ReceiverId,
                request.Sdp,
                request.SdpType,
                request.Timestamp
            );
            await _mediator.Send(command);
        }

        /// <summary>
        /// ICE 候选交换
        /// </summary>
        public async Task IceCandidateExchange(IceCandidateExchangeRequest request)
        {
            var command = new IceCandidateExchangeCommand(
                request.CallId,
                request.SenderId,
                request.ReceiverId,
                request.Candidate,
                request.SdpMid,
                request.SdpMLineIndex,
                request.Timestamp
            );
            await _mediator.Send(command);
        }
    }
}