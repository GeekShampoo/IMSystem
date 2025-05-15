using IMSystem.Server.Domain.Common;
// User 实体与 MessageReadReceipt 在同一个 IMSystem.Server.Domain.Entities 命名空间下
using System;

namespace IMSystem.Server.Domain.Entities;

/// <summary>
/// 表示消息的已读回执。
/// </summary>
public class MessageReadReceipt : AuditableEntity
{
    /// <summary>
    /// 获取或设置关联的消息ID。
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// 获取或设置读取该消息的用户ID。
    /// </summary>
    public Guid ReaderUserId { get; private set; }

    /// <summary>
    /// 获取或设置消息被读取的时间。
    /// </summary>
    public DateTimeOffset ReadAt { get; private set; }

    // 导航属性 (可选，根据需要添加)
    // public virtual Message Message { get; private set; }
    // public virtual User ReaderUser { get; private set; }

    /// <summary>
    /// 私有构造函数，供EF Core使用。
    /// </summary>
    private MessageReadReceipt() { }

    /// <summary>
    /// 创建一个新的消息已读回执实例。
    /// </summary>
    /// <param name="messageId">消息ID。</param>
    /// <param name="readerUserId">读取者用户ID。</param>
    /// <param name="readAt">读取时间。</param>
    public MessageReadReceipt(Guid messageId, Guid readerUserId, DateTimeOffset readAt)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        if (readerUserId == Guid.Empty)
            throw new ArgumentException("Reader User ID cannot be empty.", nameof(readerUserId));

        MessageId = messageId;
        ReaderUserId = readerUserId; // Specific property for clarity in this domain context
        ReadAt = readAt;

        // Align with AuditableEntity properties
        CreatedBy = readerUserId;
        CreatedAt = readAt; // The time the receipt is created is the time it was read
        LastModifiedAt = CreatedAt;
        LastModifiedBy = CreatedBy;
    }

    /// <summary>
    /// 创建已读回执的工厂方法。
    /// </summary>
    /// <param name="messageId">消息 ID。</param>
    /// <param name="readerUserId">读取者用户 ID。</param>
    /// <returns>新的 MessageReadReceipt 实例。</returns>
    public static MessageReadReceipt Create(Guid messageId, Guid readerUserId)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        if (readerUserId == Guid.Empty)
            throw new ArgumentException("Reader User ID cannot be empty.", nameof(readerUserId));

        // The constructor now handles setting CreatedBy and CreatedAt.
        // ReadAt will be UtcNow, and CreatedAt will align with it.
        return new MessageReadReceipt(messageId, readerUserId, DateTimeOffset.UtcNow);
    }
}