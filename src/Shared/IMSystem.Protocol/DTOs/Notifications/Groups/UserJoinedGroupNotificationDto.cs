using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 用户加入群组通知DTO
    /// </summary>
    public class UserJoinedGroupNotificationDto
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
        /// 加入群组的用户ID
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 加入群组的用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// 用户头像URL
        /// </summary>
        public string? UserAvatarUrl { get; set; }
        
        /// <summary>
        /// 用户在群组中的角色
        /// </summary>
        public ProtocolGroupUserRole Role { get; set; }
        
        /// <summary>
        /// 加入时间
        /// </summary>
        public DateTimeOffset JoinedAt { get; set; }
        
        /// <summary>
        /// 邀请人ID（如果是被邀请加入）
        /// </summary>
        public Guid? InviterId { get; set; }
        
        /// <summary>
        /// 邀请人用户名
        /// </summary>
        public string? InviterUsername { get; set; }
    }
}