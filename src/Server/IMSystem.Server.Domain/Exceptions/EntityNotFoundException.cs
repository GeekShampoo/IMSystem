using System;

namespace IMSystem.Server.Domain.Exceptions
{
    /// <summary>
    /// 当尝试访问或操作一个不存在的领域实体时抛出的异常。
    /// </summary>
    public class EntityNotFoundException : DomainException
    {
        /// <summary>
        /// 未找到的实体的名称。
        /// </summary>
        public string EntityName { get; }

        /// <summary>
        /// 未找到的实体的标识符。
        /// </summary>
        public object EntityId { get; }

        /// <summary>
        /// 初始化 <see cref="EntityNotFoundException"/> 类的新实例。
        /// </summary>
        /// <param name="entityName">未找到的实体的名称 (例如, "User", "Order")。</param>
        /// <param name="entityId">未找到的实体的标识符。</param>
        public EntityNotFoundException(string entityName, object entityId)
            : base($"未能找到实体 '{entityName}' (ID: {entityId})。")
        {
            EntityName = entityName;
            EntityId = entityId;
        }

        /// <summary>
        /// 使用指定的错误消息、实体名称和实体ID初始化 <see cref="EntityNotFoundException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        /// <param name="entityName">未找到的实体的名称。</param>
        /// <param name="entityId">未找到的实体的标识符。</param>
        public EntityNotFoundException(string message, string entityName, object entityId)
            : base(message)
        {
            EntityName = entityName;
            EntityId = entityId;
        }

        /// <summary>
        /// 使用指定的错误消息、内部异常引用、实体名称和实体ID初始化 <see cref="EntityNotFoundException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        /// <param name="innerException">导致当前异常的异常。</param>
        /// <param name="entityName">未找到的实体的名称。</param>
        /// <param name="entityId">未找到的实体的标识符。</param>
        public EntityNotFoundException(string message, Exception innerException, string entityName, object entityId)
            : base(message, innerException)
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }
}