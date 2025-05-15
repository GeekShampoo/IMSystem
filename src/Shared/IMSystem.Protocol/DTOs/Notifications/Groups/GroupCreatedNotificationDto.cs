using System;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 群组创建成功通知DTO
    /// </summary>
    public class GroupCreatedNotificationDto
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
        /// 创建者用户ID
        /// </summary>
        public Guid CreatorUserId { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        
        // 可以根据客户端需求添加其他字段，如 AvatarUrl, Description 等
    }
}