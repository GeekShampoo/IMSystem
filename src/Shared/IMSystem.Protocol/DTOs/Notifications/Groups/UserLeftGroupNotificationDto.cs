using System;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 用户离开群组通知DTO
    /// </summary>
    public class UserLeftGroupNotificationDto
    {
        /// <summary>
        /// 群组ID
        /// </summary>
        public Guid GroupId { get; set; }
        
        /// <summary>
        /// 群组名称
        /// </summary>
        public string GroupName { get; set; } = string.Empty;
        
        /// <summary>
        /// 离开的用户ID
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 离开的用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// 离开原因/方式（主动离开、被踢出等）
        /// </summary>
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作者ID（如果是被踢出）
        /// </summary>
        public Guid? ActorId { get; set; }
        
        /// <summary>
        /// 操作者名称
        /// </summary>
        public string? ActorName { get; set; }
        
        /// <summary>
        /// 离开时间
        /// </summary>
        public DateTimeOffset LeftAt { get; set; }
    }
}