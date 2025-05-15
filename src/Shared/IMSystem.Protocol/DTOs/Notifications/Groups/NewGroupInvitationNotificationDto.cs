using System;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 群组邀请通知DTO
    /// </summary>
    public class NewGroupInvitationNotificationDto
    {
        /// <summary>
        /// 邀请ID
        /// </summary>
        public Guid InvitationId { get; set; }
        
        /// <summary>
        /// 群组ID
        /// </summary>
        public Guid GroupId { get; set; }
        
        /// <summary>
        /// 群组名称
        /// </summary>
        public string GroupName { get; set; } = string.Empty;
        
        /// <summary>
        /// 邀请人ID
        /// </summary>
        public Guid InviterId { get; set; }
        
        /// <summary>
        /// 邀请人用户名
        /// </summary>
        public string InviterUsername { get; set; } = string.Empty; // 统一为 InviterUsername
        
        /// <summary>
        /// 邀请消息
        /// </summary>
        public string? Message { get; set; }
        
        /// <summary>
        /// 邀请创建时间
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } // 统一为 CreatedAt 或 InvitedAt
        
        /// <summary>
        /// 邀请过期时间
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }
        
        /// <summary>
        /// 邀请者昵称，便于客户端显示
        /// </summary>
        public string? InviterNickname { get; set; }
        
        /// <summary>
        /// 邀请者头像URL，便于客户端显示
        /// </summary>
        public string? InviterAvatarUrl { get; set; }
    }
}