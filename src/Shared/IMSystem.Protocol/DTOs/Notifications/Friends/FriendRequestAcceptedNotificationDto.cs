using System;

namespace IMSystem.Protocol.DTOs.Notifications.Friends
{
    /// <summary>
    /// 好友请求接受通知DTO
    /// </summary>
    public class FriendRequestAcceptedNotificationDto
    {
        /// <summary>
        /// 好友关系ID
        /// </summary>
        public Guid FriendshipId { get; set; }
        
        /// <summary>
        /// 接受请求的用户ID
        /// </summary>
        public Guid AcceptorId { get; set; } // 统一使用 Acceptor
        
        /// <summary>
        /// 接受请求的用户名
        /// </summary>
        public string AcceptorUsername { get; set; } = string.Empty; // 确保有默认值
        
        /// <summary>
        /// 接受者设置的备注名
        /// </summary>
        public string? AcceptorRemark { get; set; } // 备注可选
        
        /// <summary>
        /// 请求接受时间
        /// </summary>
        public DateTimeOffset AcceptedAt { get; set; }
        
        /// <summary>
        /// 接受者昵称，便于客户端直接显示
        /// </summary>
        public string? AcceptorNickname { get; set; }
        
        /// <summary>
        /// 接受者头像URL，便于客户端直接显示
        /// </summary>
        public string? AcceptorAvatarUrl { get; set; }
    }
}