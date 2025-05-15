using System;
using MediatR;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Signaling.Commands
{
    /// <summary>
    /// ICE 候选交换命令
    /// </summary>
    public class IceCandidateExchangeCommand : IRequest<Result>
    {
        public Guid CallId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Candidate { get; set; }
        public string SdpMid { get; set; }
        public int SdpMLineIndex { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public IceCandidateExchangeCommand(Guid callId, Guid senderId, Guid receiverId, string candidate, string sdpMid, int sdpMLineIndex, DateTimeOffset timestamp)
        {
            CallId = callId;
            SenderId = senderId;
            ReceiverId = receiverId;
            Candidate = candidate;
            SdpMid = sdpMid;
            SdpMLineIndex = sdpMLineIndex;
            Timestamp = timestamp;
        }
    }
}