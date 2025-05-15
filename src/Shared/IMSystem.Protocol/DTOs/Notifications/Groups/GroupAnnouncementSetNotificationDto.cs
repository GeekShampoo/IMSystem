using System;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 群组公告设置通知DTO
    /// </summary>
    public class GroupAnnouncementSetNotificationDto
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
        /// 公告内容
        /// </summary>
        public string? Announcement { get; set; }
        
        /// <summary>
        /// 设置公告的用户ID
        /// </summary>
        public Guid ActorUserId { get; set; }
        
        /// <summary>
        /// 设置公告的用户名
        /// </summary>
        public string ActorUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// 公告设置时间
        /// </summary>
        public DateTimeOffset? AnnouncementSetAt { get; set; }
    }
}