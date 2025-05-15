using System;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 用于标识用户的简单DTO
    /// </summary>
    public class UserIdentifierDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    /// 群组所有权转移通知DTO
    /// </summary>
    public class GroupOwnershipTransferredNotificationDto
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
        /// 原群主信息
        /// </summary>
        public UserIdentifierDto? OldOwner { get; set; }
        
        /// <summary>
        /// 新群主信息
        /// </summary>
        public UserIdentifierDto? NewOwner { get; set; }
        
        /// <summary>
        /// 发起转让操作的用户ID (通常是旧群主)
        /// </summary>
        public Guid ActorUserId { get; set; }
        
        /// <summary>
        /// 发起转让操作的用户名
        /// </summary>
        public string ActorUsername { get; set; } = string.Empty;
    }
}