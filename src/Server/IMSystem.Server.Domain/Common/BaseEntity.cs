using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // Required for NotMapped

namespace IMSystem.Server.Domain.Common
{
    /// <summary>
    /// 所有领域实体的基类。
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 实体的唯一标识符。
        /// </summary>
        public Guid Id { get; protected set; } = Guid.NewGuid(); // 默认为新生成的 GUID

        private readonly List<DomainEvent> _domainEvents = new List<DomainEvent>();

        /// <summary>
        /// 与此实体相关的领域事件集合（只读）。
        /// 此属性不应被 EF Core 映射到数据库。
        /// </summary>
        [NotMapped]
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Gets or sets the row version for optimistic concurrency control.
        /// This property will be configured as a concurrency token by EF Core.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        // EF Core will automatically manage this property when configured as a row version.
        // It's typically a byte[] (timestamp/rowversion in SQL Server).
        public byte[] RowVersion { get; protected set; }
#pragma warning restore CS8618

        /// <summary>
        /// 添加一个领域事件到此实体。
        /// </summary>
        /// <param name="domainEvent">要添加的领域事件。</param>
        public void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// 移除一个领域事件从此实体。
        /// </summary>
        /// <param name="domainEvent">要移除的领域事件。</param>
        public void RemoveDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        /// <summary>
        /// 清空此实体上的所有领域事件。
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        // 可选：重写 Equals 和 GetHashCode 以基于 Id 进行比较
        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            BaseEntity other = (BaseEntity)obj;
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(BaseEntity? a, BaseEntity? b)
        {
            if (a is null && b is null)
                return true;

            if (a is null || b is null)
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(BaseEntity? a, BaseEntity? b)
        {
            return !(a == b);
        }
    }
}