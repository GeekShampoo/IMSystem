using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Responses.Friends
{
    /// <summary>
    /// 表示好友信息的响应数据传输对象。
    /// </summary>
    public class FriendDto
    {
        /// <summary>
        /// 获取或设置好友的用户ID。
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 获取或设置好友的用户名。
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置好友的头像URL（可选）。
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// 获取或设置好友是否在线。
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// 获取或设置好友的自定义状态（可选）。
        /// </summary>
        public string? CustomStatus { get; set; }

        /// <summary>
        /// 获取或设置好友的最后在线时间（可选）。
        /// </summary>
        public DateTimeOffset? LastSeenAt { get; set; }

        /// <summary>
        /// 获取或设置对应 Friendship 实体的ID。
        /// </summary>
        public Guid FriendshipId { get; set; }

        /// <summary>
        /// 获取或设置好友关系的状态 (例如 "Pending", "Accepted", "Declined", "Blocked")。
        /// </summary>
        public ProtocolFriendStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the remark name that the current user has set for this friend.
        /// </summary>
        public string? RemarkName { get; set; }
    }
}