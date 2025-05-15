using System;
using IMSystem.Protocol.Common;
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// 邀请用户加入群组的命令。
/// </summary>
public class InviteUserToGroupCommand : IRequest<Result<Guid>> // 返回创建的 GroupInvitation 记录的 ID
{
    /// <summary>
    /// 目标群组ID。
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// 被邀请用户的ID。
    /// </summary>
    public Guid InvitedUserId { get; }

    /// <summary>
    /// 发出邀请的用户的ID (从认证上下文中获取)。
    /// </summary>
    public Guid InviterUserId { get; }

    /// <summary>
    /// 邀请时附带的消息 (可选)。
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// 邀请的过期时间 (可选)。
    /// </summary>
    public DateTime? ExpiresAt { get; }

    public InviteUserToGroupCommand(Guid groupId, Guid invitedUserId, Guid inviterUserId, string? message = null, DateTime? expiresAt = null)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("群组ID不能为空。", nameof(groupId));
        if (invitedUserId == Guid.Empty)
            throw new ArgumentException("被邀请用户ID不能为空。", nameof(invitedUserId));
        if (inviterUserId == Guid.Empty)
            throw new ArgumentException("邀请者ID不能为空。", nameof(inviterUserId));
        if (invitedUserId == inviterUserId) // 通常不允许邀请自己，但具体逻辑可能在 Handler 中处理
            throw new ArgumentException("不能邀请自己加入群组。", nameof(invitedUserId));

        GroupId = groupId;
        InvitedUserId = invitedUserId;
        InviterUserId = inviterUserId;
        Message = message;
        ExpiresAt = expiresAt;
    }
}