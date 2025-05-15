using System;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 操作者信息DTO
    /// </summary>
    public class ActorDetailsDto
    {
        /// <summary>
        /// 操作者用户ID
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 操作者用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    /// 群组被删除通知DTO
    /// </summary>
    public class GroupDeletedNotificationDto
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
        /// 操作者信息
        /// </summary>
        public ActorDetailsDto Actor { get; set; }
        
        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTimeOffset DeletedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}