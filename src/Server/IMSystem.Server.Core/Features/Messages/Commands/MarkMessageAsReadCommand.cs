using MediatR;
using System;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Messages.Commands;

/// <summary>
/// 标记消息为已读的命令。
/// </summary>
public class MarkMessageAsReadCommand : IRequest<Result>
{
    /// <summary>
    /// 执行此操作的用户（读取者）的ID。
    /// </summary>
    public Guid ReaderUserId { get; }

    /// <summary>
    /// 聊天对象的ID (用于用户间聊天)。
    /// </summary>
    public Guid? ChatPartnerId { get; }

    /// <summary>
    /// 群组ID (用于群组聊天)。
    /// </summary>
    public Guid? GroupId { get; }

    /// <summary>
    /// 可选。如果提供，则将此消息ID（包括）之前的所有消息标记为已读。
    /// </summary>
    public Guid? UpToMessageId { get; }

    /// <summary>
    /// 可选。如果提供，则将此时间戳之前的所有消息标记为已读。
    /// 作为 UpToMessageId 的替代方案。
    /// </summary>
    public DateTimeOffset? LastReadTimestamp { get; }

    public MarkMessageAsReadCommand(Guid readerUserId, Guid? chatPartnerId, Guid? groupId, Guid? upToMessageId, DateTimeOffset? lastReadTimestamp)
    {
        ReaderUserId = readerUserId;
        ChatPartnerId = chatPartnerId;
        GroupId = groupId;
        UpToMessageId = upToMessageId;
        LastReadTimestamp = lastReadTimestamp;

        if (ReaderUserId == Guid.Empty)
        {
            throw new ArgumentException("ReaderUserId 不能为空。", nameof(readerUserId));
        }

        if (!chatPartnerId.HasValue && !groupId.HasValue)
        {
            throw new ArgumentException("必须提供 ChatPartnerId 或 GroupId 中的一个。", $"{nameof(chatPartnerId)}/{nameof(groupId)}");
        }

        if (chatPartnerId.HasValue && groupId.HasValue)
        {
            throw new ArgumentException("不能同时提供 ChatPartnerId 和 GroupId。", $"{nameof(chatPartnerId)}/{nameof(groupId)}");
        }

        // 可以选择添加更多验证，例如 UpToMessageId 和 LastReadTimestamp 不应同时提供，或者至少提供一个范围指示。
        // 但这也可以在 Handler 中处理。
    }
}