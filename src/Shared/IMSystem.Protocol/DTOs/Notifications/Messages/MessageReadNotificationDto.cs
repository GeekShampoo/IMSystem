using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Notifications;

/// <summary>
/// 服务端向客户端推送的消息已读通知。
/// </summary>
public class MessageReadNotificationDto
{
    /// <summary>
    /// 获取或设置被读取的消息ID。
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// 获取或设置读取该消息的用户ID。
    /// </summary>
    public Guid ReaderUserId { get; set; }

    /// <summary>
    /// 获取或设置读取该消息的用户的用户名 (可选,方便客户端显示)。
    /// </summary>
    public string? ReaderUsername { get; set; }

    /// <summary>
    /// 获取或设置消息被读取的时间。
    /// </summary>
    public DateTimeOffset ReadAt { get; set; }

    /// <summary>
    /// 获取或设置对话ID或上下文标识 (例如，如果是单聊，可以是对方用户ID；如果是群聊，可以是群组ID)。
    /// 这有助于客户端确定在哪个聊天窗口更新已读状态。
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// 标识此已读回执是针对单聊还是群聊。
    /// "User" 或 "Group"
    /// </summary>
    public ProtocolChatType ConversationType { get; set; }
}