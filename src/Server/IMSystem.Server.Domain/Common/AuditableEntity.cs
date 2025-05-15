using System;

namespace IMSystem.Server.Domain.Common
{
    /// <summary>
    /// 提供审计属性（如创建和修改信息）的实体基类。
    /// </summary>
    public abstract class AuditableEntity : BaseEntity
    {
        /// <summary>
        /// 实体创建的日期和时间（UTC）。
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// 创建此实体的用户的标识符（可选）。
        /// </summary>
        public Guid? CreatedBy { get; set; } // 可空，因为创建者可能未知或不适用

        /// <summary>
        /// 实体最后修改的日期和时间（UTC，可选）。
        /// </summary>
        public DateTimeOffset? LastModifiedAt { get; set; }

        /// <summary>
        /// 最后修改此实体的用户的标识符（可选）。
        /// </summary>
        public Guid? LastModifiedBy { get; set; }

        protected AuditableEntity()
        {
            CreatedAt = DateTimeOffset.UtcNow;
            // LastModifiedAt can be set when the entity is actually modified.
        }
    }
}