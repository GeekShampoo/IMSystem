using System;

namespace IMSystem.Protocol.DTOs.Responses.Friends;

/// <summary>
/// 代表好友的简要信息，通常用于好友列表或分组内的好友展示。
/// </summary>
public class FriendSummaryDto
{
    /// <summary>
    /// 好友的用户ID (对方的UserID)。
    /// </summary>
    public Guid FriendUserId { get; set; }

    /// <summary>
    /// 这段好友关系的唯一标识ID。
    /// </summary>
    public Guid FriendshipId { get; set; }

    /// <summary>
    /// 好友的用户名。
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 好友的昵称 (可选)。
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 好友的头像URL (可选)。
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 当前登录用户对该好友的备注名 (可选)。
    /// </summary>
    public string? RemarkName { get; set; }

    /// <summary>
    /// Indicates if the friend is currently online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Friend's custom status message (optional).
    /// </summary>
    public string? CustomStatus { get; set; }

    /// <summary>
    /// The last time the friend was seen online (optional).
    /// </summary>
    public DateTimeOffset? LastSeenAt { get; set; }
}