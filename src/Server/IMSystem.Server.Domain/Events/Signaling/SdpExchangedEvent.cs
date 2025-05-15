using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Signaling
{
    /// <summary>
    /// 表示一次 SDP 交换已完成的领域事件。
    /// </summary>
    public class SdpExchangedEvent : DomainEvent
    {
        public Guid CallId { get; }
        public Guid SenderId { get; }
        public Guid ReceiverId { get; }
        public string Sdp { get; }
        public string SdpType { get; }
        public DateTimeOffset Timestamp { get; }

        public SdpExchangedEvent(Guid callId, Guid senderId, Guid receiverId, string sdp, string sdpType, DateTimeOffset timestamp)
            : base(entityId: callId, triggeredBy: senderId)
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