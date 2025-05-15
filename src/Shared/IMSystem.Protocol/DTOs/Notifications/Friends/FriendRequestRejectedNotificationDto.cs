using System;

namespace IMSystem.Protocol.DTOs.Notifications.Friends
{
    /// <summary>
    /// 好友请求被拒绝通知DTO
    /// </summary>
    public class FriendRequestRejectedNotificationDto
    {
        /// <summary>
        /// 好友关系ID
        /// </summary>
        public Guid FriendshipId { get; set; }
        
        /// <summary>
        /// 拒绝请求的用户ID
        /// </summary>
        public Guid RejecterId { get; set; }
        
        /// <summary>
        /// 拒绝请求的用户名
        /// </summary>
        public string RejecterName { get; set; } = string.Empty;
        
        /// <summary>
        /// 拒绝原因（如果有）
        /// </summary>
        public string? Reason { get; set; }
        
        /// <summary>
        /// 拒绝时间
        /// </summary>
        public DateTimeOffset RejectedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}