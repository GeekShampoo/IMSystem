using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Signaling
{
    /// <summary>
    /// 表示一次呼叫邀请已发出的领域事件。
    /// </summary>
    public class CallInvitedEvent : DomainEvent
    {
        public Guid CallId { get; }
        public Guid CallerId { get; }
        public Guid CalleeId { get; }
        public string CallType { get; }
        public DateTimeOffset Timestamp { get; }

        public CallInvitedEvent(Guid callId, Guid callerId, Guid calleeId, string callType, DateTimeOffset timestamp)
            : base(entityId: callId, triggeredBy: callerId)
        {
            CallId = callId;
            CallerId = callerId;
            CalleeId = calleeId;
            CallType = callType;
            Timestamp = timestamp;
        }
    }
}