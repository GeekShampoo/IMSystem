using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 群组成员角色更新通知DTO
    /// </summary>
    public class GroupMemberRoleUpdatedNotificationDto
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
        /// 目标成员用户ID
        /// </summary>
        public Guid TargetMemberUserId { get; set; }
        
        /// <summary>
        /// 目标成员用户名
        /// </summary>
        public string TargetMemberUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// 原角色
        /// </summary>
        public ProtocolGroupUserRole OldRole { get; set; }
        
        /// <summary>
        /// 新角色
        /// </summary>
        public ProtocolGroupUserRole NewRole { get; set; }
        
        /// <summary>
        /// 执行操作的用户ID
        /// </summary>
        public Guid ActorUserId { get; set; }
        
        /// <summary>
        /// 执行操作的用户名
        /// </summary>
        public string ActorUsername { get; set; } = string.Empty;
    }
}