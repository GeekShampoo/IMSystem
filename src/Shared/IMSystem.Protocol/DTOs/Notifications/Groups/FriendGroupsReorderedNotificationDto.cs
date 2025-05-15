using System;
using System.Collections.Generic;

namespace IMSystem.Protocol.DTOs.Notifications.Groups
{
    /// <summary>
    /// 通知客户端好友分组顺序已更改。
    /// </summary>
    public class FriendGroupsReorderedNotificationDto
    {
        /// <summary>
        /// 用户ID。
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 包含分组ID及其新顺序的列表。
        /// </summary>
        public List<FriendGroupOrderItemDto> ReorderedGroups { get; set; } = new List<FriendGroupOrderItemDto>();
    }

    /// <summary>
    /// 表示好友分组排序项的DTO。
    /// </summary>
    public class FriendGroupOrderItemDto
    {
        /// <summary>
        /// 分组ID。
        /// </summary>
        public Guid GroupId { get; set; }

        /// <summary>
        /// 新的排序序号。
        /// </summary>
        public int NewOrder { get; set; }
    }
}