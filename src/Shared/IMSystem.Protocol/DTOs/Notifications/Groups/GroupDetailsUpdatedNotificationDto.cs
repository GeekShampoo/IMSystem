using System;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 群组信息更新通知DTO
    /// </summary>
    public class GroupDetailsUpdatedNotificationDto
    {
        /// <summary>
        /// 群组ID
        /// </summary>
        public Guid GroupId { get; set; }
        
        /// <summary>
        /// 执行更新的用户ID
        /// </summary>
        public Guid UpdaterId { get; set; }
        
        /// <summary>
        /// 群组名称
        /// </summary>
        public string GroupName { get; set; }
        
        /// <summary>
        /// 群组描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 群组头像URL
        /// </summary>
        public string AvatarUrl { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; }
    }
}