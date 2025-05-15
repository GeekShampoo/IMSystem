using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Signaling
{
    /// <summary>
    /// 表示一次呼叫已被应答的领域事件。
    /// </summary>
    public class CallAnsweredEvent : DomainEvent
    {
        public Guid CallId { get; }
        public Guid CallerId { get; }
        public Guid CalleeId { get; }
        public bool Accepted { get; }
        public DateTimeOffset Timestamp { get; }

        public CallAnsweredEvent(Guid callId, Guid callerId, Guid calleeId, bool accepted, DateTimeOffset timestamp)
            : base(entityId: callId, triggeredBy: calleeId) // 应答方(callee)是触发者
        {
            CallId = callId;
            CallerId = callerId;
            CalleeId = calleeId;
            Accepted = accepted;
            Timestamp = timestamp;
        }
    }
}