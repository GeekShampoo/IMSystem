using System;
using IMSystem.Protocol.Enums;
using MediatR;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Signaling.Commands
{
    /// <summary>
    /// SDP 交换命令
    /// </summary>
    public class SdpExchangeCommand : IRequest<Result>
    {
        public Guid CallId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Sdp { get; set; }
        public SdpType SdpType { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public SdpExchangeCommand(Guid callId, Guid senderId, Guid receiverId, string sdp, SdpType sdpType, DateTimeOffset timestamp)
        {
            CallId = callId;
            SenderId = senderId;
            ReceiverId = receiverId;
            Sdp = sdp;
            SdpType = sdpType;
            Timestamp = timestamp;
        }
    }
}