using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Signaling
{
    /// <summary>
    /// 表示一次 ICE Candidate 交换已完成的领域事件。
    /// </summary>
    public class IceCandidateExchangedEvent : DomainEvent
    {
        public Guid CallId { get; }
        public Guid SenderId { get; }
        public Guid ReceiverId { get; }
        public string Candidate { get; }
        public string SdpMid { get; }
        public int SdpMLineIndex { get; }
        public DateTimeOffset Timestamp { get; }

        public IceCandidateExchangedEvent(Guid callId, Guid senderId, Guid receiverId, string candidate, string sdpMid, int sdpMLineIndex, DateTimeOffset timestamp)
            : base(entityId: callId, triggeredBy: senderId)
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