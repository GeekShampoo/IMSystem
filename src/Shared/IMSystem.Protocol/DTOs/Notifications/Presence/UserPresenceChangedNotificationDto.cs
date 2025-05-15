using System;

namespace IMSystem.Protocol.DTOs.Notifications
{
    /// <summary>
    /// 用户在线状态变更通知DTO
    /// </summary>
    public class UserPresenceChangedNotificationDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 是否在线
        /// </summary>
        public bool IsOnline { get; set; }
        
        /// <summary>
        /// 用户自定义状态
        /// </summary>
        public string? CustomStatus { get; set; }
        
        /// <summary>
        /// 最后一次在线时间
        /// </summary>
        public DateTimeOffset? LastSeenAt { get; set; }
    }
}