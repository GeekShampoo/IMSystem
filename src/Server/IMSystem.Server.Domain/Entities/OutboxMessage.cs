using System;

namespace IMSystem.Server.Domain.Entities
{
    /// <summary>
    /// Outbox 消息实体，用于存储待发布的领域事件
    /// </summary>
    public class OutboxMessage
    {
        public Guid Id { get; private set; }
        public string EventType { get; private set; } // 领域事件的类型名称
        public string EventPayload { get; private set; } // 序列化后的领域事件内容 (例如 JSON)
        public DateTimeOffset OccurredAt { get; private set; } // 事件发生时间
        public DateTimeOffset? ProcessedAt { get; private set; } // 事件处理完成时间 (null 表示尚未处理)
        public string? Error { get; private set; } // 处理过程中发生的错误信息 (可选)
        public int RetryCount { get; private set; } // 重试次数
        
        // 新增字段，与增强后的DomainEvent保持一致
        public Guid EventId { get; private set; } // 事件的唯一标识符
        public int Version { get; private set; } // 事件版本号
        public Guid? EntityId { get; private set; } // 关联的实体ID
        public Guid? TriggeredBy { get; private set; } // 触发事件的用户ID

        // EF Core 使用的私有构造函数
        private OutboxMessage() { }

        public OutboxMessage(string eventType, string eventPayload, DateTimeOffset occurredAt, 
            Guid eventId, int version = 1, Guid? entityId = null, Guid? triggeredBy = null)
        {
            Id = Guid.NewGuid();
            EventType = eventType;
            EventPayload = eventPayload;
            OccurredAt = occurredAt;
            ProcessedAt = null;
            Error = null;
            RetryCount = 0;
            
            // 设置新增字段
            EventId = eventId;
            Version = version;
            EntityId = entityId;
            TriggeredBy = triggeredBy;
        }

        // 为了保持向后兼容性，保留原有构造函数
        public OutboxMessage(string eventType, string eventPayload, DateTimeOffset occurredAt)
            : this(eventType, eventPayload, occurredAt, Guid.NewGuid())
        {
        }

        public void MarkAsProcessed()
        {
            ProcessedAt = DateTimeOffset.UtcNow;
            Error = null; // 清除之前的错误（如果存在）
        }

        public void MarkAsFailed(string errorMessage)
        {
            ProcessedAt = DateTimeOffset.UtcNow; // 也可以认为处理尝试已完成，但失败
            Error = errorMessage;
            // RetryCount is incremented separately by the processor service.
        }

        public void IncrementRetryCount()
        {
            RetryCount++;
        }
    }
}