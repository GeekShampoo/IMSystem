using IMSystem.Protocol.Common; // For Result
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Friends.Commands;

/// <summary>
/// 发送好友请求的命令。
/// </summary>
public class SendFriendRequestCommand : IRequest<Result<Guid>> // 返回创建的 Friendship 记录的 ID
{
    /// <summary>
    /// 发送请求的用户ID (从认证上下文中获取)。
    /// </summary>
    public Guid RequesterId { get; }

    /// <summary>
    /// 接收请求的目标用户ID。
    /// </summary>
    public Guid AddresseeId { get; }

    /// <summary>
    /// 请求者发送的备注/验证信息（可选）。
    /// </summary>
    public string? RequesterRemark { get; }

    public SendFriendRequestCommand(Guid requesterId, Guid addresseeId, string? requesterRemark = null)
    {
        if (requesterId == Guid.Empty)
            throw new ArgumentException("请求者ID不能为空。", nameof(requesterId));
        if (addresseeId == Guid.Empty)
            throw new ArgumentException("接收者ID不能为空。", nameof(addresseeId));
        if (requesterId == addresseeId)
            throw new ArgumentException("不能向自己发送好友请求。", nameof(addresseeId));
            
        RequesterId = requesterId;
        AddresseeId = addresseeId;
        RequesterRemark = requesterRemark;
    }
}