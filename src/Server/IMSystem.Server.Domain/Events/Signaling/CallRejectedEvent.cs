using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Signaling
{
    /// <summary>
    /// 表示一次呼叫被拒绝的领域事件。
    /// </summary>
    public class CallRejectedEvent : DomainEvent
    {
        public Guid CallId { get; }
        public Guid CallerId { get; }
        public Guid CalleeId { get; }
        public string Reason { get; }
        public DateTimeOffset Timestamp { get; }

        public CallRejectedEvent(Guid callId, Guid callerId, Guid calleeId, string reason, DateTimeOffset timestamp)
            : base(entityId: callId, triggeredBy: calleeId) // 被叫方（拒绝电话的人）是触发者
        {
            CallId = callId;
            CallerId = callerId;
            CalleeId = calleeId;
            Reason = reason;
            Timestamp = timestamp;
        }
    }
}