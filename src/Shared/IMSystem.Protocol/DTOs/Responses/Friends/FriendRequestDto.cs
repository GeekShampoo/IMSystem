using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Responses.Friends;

/// <summary>
/// 表示一个好友请求的数据传输对象。
/// </summary>
public class FriendRequestDto
{
    /// <summary>
    /// 好友请求的唯一标识符 (即 FriendshipId)。
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// 发送好友请求的用户的ID。
    /// </summary>
    public Guid RequesterId { get; set; }

    /// <summary>
    /// 发送好友请求的用户的用户名。
    /// </summary>
    public string RequesterUsername { get; set; } = null!;

    /// <summary>
    /// 发送好友请求的用户的个人资料图片URL (可选)。
    /// </summary>
    public string? RequesterProfilePictureUrl { get; set; }

    /// <summary>
    /// 请求发送的时间。
    /// </summary>
    public DateTimeOffset RequestedAt { get; set; }

    /// <summary>
    /// 请求状态 (通常应为 Pending)。
    /// </summary>
    public ProtocolFriendStatus Status { get; set; }
}