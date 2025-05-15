using System;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 通知客户端好友分组信息已更新。
    /// </summary>
    public class FriendGroupUpdatedNotificationDto
    {
        /// <summary>
        /// 分组ID。
        /// </summary>
        public Guid GroupId { get; set; }

        /// <summary>
        /// 新的分组名称。
        /// </summary>
        public string NewName { get; set; } = string.Empty;

        /// <summary>
        /// 新的排序序号。
        /// </summary>
        public int NewOrder { get; set; }

        /// <summary>
        /// 是否为默认分组。
        /// </summary>
        public bool IsDefault { get; set; }
    }
}