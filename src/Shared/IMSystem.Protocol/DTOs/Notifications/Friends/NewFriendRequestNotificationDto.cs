using System;

namespace IMSystem.Protocol.DTOs.Notifications.Friends
{
    /// <summary>
    /// 新好友请求通知DTO
    /// </summary>
    public class NewFriendRequestNotificationDto
    {
        /// <summary>
        /// 好友关系ID
        /// </summary>
        public Guid FriendshipId { get; set; }
        
        /// <summary>
        /// 请求者ID
        /// </summary>
        public Guid RequesterId { get; set; }
        
        /// <summary>
        /// 请求者用户名
        /// </summary>
        public string RequesterUsername { get; set; } = string.Empty; // 统一使用 RequesterUsername
        
        /// <summary>
        /// 请求者附言或备注
        /// </summary>
        public string? RequesterRemark { get; set; } // 请求备注
        
        /// <summary>
        /// 请求发起时间
        /// </summary>
        public DateTimeOffset RequestedAt { get; set; } // 统一使用 RequestedAt
        
        /// <summary>
        /// 请求者昵称，便于客户端显示
        /// </summary>
        public string? RequesterNickname { get; set; }
        
        /// <summary>
        /// 请求者头像URL，便于客户端显示
        /// </summary>
        public string? RequesterAvatarUrl { get; set; }
        
        /// <summary>
        /// 请求过期时间（如果好友请求有过期概念）
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}