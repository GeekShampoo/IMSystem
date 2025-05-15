using System;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 群组成员被踢出通知DTO
    /// </summary>
    public class GroupMemberKickedNotificationDto
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
        /// 被踢出的用户ID
        /// </summary>
        public Guid KickedUserId { get; set; }
        
        /// <summary>
        /// 被踢出的用户名
        /// </summary>
        public string KickedUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// 执行踢出操作的用户ID
        /// </summary>
        public Guid ActorUserId { get; set; }
        
        /// <summary>
        /// 执行踢出操作的用户名
        /// </summary>
        public string ActorUsername { get; set; } = string.Empty;
    }
}