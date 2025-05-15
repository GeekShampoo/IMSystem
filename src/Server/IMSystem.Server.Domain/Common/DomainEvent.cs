using MediatR; // Domain events are often MediatR notifications
using System;

namespace IMSystem.Server.Domain.Common
{
    /// <summary>
    /// 表示一个领域事件的抽象基类。
    /// 领域事件是领域中发生的一些重要事情。
    /// </summary>
    public abstract class DomainEvent : INotification
    {
        /// <summary>
        /// 事件的唯一标识符。
        /// </summary>
        public Guid EventId { get; }

        /// <summary>
        /// 事件发生的日期和时间（UTC）。
        /// </summary>
        public DateTimeOffset DateOccurred { get; }

        /// <summary>
        /// 事件的版本号，用于事件演化。
        /// </summary>
        public int Version { get; protected set; } = 1;

        /// <summary>
        /// 事件相关的实体ID，可选。
        /// </summary>
        public Guid? EntityId { get; protected set; }

        /// <summary>
        /// 触发事件的用户ID，可选。
        /// </summary>
        public Guid? TriggeredBy { get; protected set; }

        protected DomainEvent()
        {
            EventId = Guid.NewGuid();
            DateOccurred = DateTimeOffset.UtcNow;
        }

        protected DomainEvent(Guid? entityId, Guid? triggeredBy = null) : this()
        {
            EntityId = entityId;
            TriggeredBy = triggeredBy;
        }
    }
}