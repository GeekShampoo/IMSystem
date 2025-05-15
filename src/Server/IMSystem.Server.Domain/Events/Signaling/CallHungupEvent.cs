using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Signaling
{
    /// <summary>
    /// 表示一次呼叫被挂断的领域事件。
    /// </summary>
    public class CallHungupEvent : DomainEvent
    {
        public Guid CallId { get; }
        public Guid CallerId { get; }
        public Guid CalleeId { get; }
        public string Reason { get; }
        public DateTimeOffset Timestamp { get; }
        
        // 添加挂断方的ID，用于表示谁挂断了电话
        public Guid InitiatorId { get; }

        public CallHungupEvent(Guid callId, Guid callerId, Guid calleeId, string reason, DateTimeOffset timestamp, Guid initiatorId)
            : base(entityId: callId, triggeredBy: initiatorId)
        {
            CallId = callId;
            CallerId = callerId;
            CalleeId = calleeId;
            Reason = reason;
            Timestamp = timestamp;
            InitiatorId = initiatorId;
        }
        
        // 为了保持向后兼容性，保留原有构造函数
        public CallHungupEvent(Guid callId, Guid callerId, Guid calleeId, string reason, DateTimeOffset timestamp)
            : this(callId, callerId, calleeId, reason, timestamp, callerId) // 默认发起者是呼叫者
        {
        }
    }
}