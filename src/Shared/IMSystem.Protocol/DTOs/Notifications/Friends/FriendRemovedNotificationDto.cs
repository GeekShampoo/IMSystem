using System;

namespace IMSystem.Protocol.DTOs.Notifications.Friends
{
    /// <summary>
    /// 好友被移除通知DTO
    /// </summary>
    public class FriendRemovedNotificationDto
    {
        /// <summary>
        /// 好友关系ID
        /// </summary>
        public Guid FriendshipId { get; set; }
        
        /// <summary>
        /// 执行移除操作的用户ID
        /// </summary>
        public Guid RemoverUserId { get; set; }
        
        /// <summary>
        /// 执行移除操作的用户名
        /// </summary>
        public string RemoverUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// 被移除的好友的用户ID
        /// </summary>
        public Guid RemovedFriendUserId { get; set; }
        
        /// <summary>
        /// 被移除的好友的用户名
        /// </summary>
        public string RemovedFriendUsername { get; set; } = string.Empty;
    }
}