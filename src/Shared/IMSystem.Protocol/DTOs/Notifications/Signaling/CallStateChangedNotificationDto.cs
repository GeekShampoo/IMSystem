using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Notifications.Signaling
{
    /// <summary>
    /// 通话状态变更通知
    /// </summary>
    public class CallStateChangedNotificationDto
    {
        /// <summary>
        /// 通话会话ID
        /// </summary>
        public Guid CallId { get; set; }

        /// <summary>
        /// 主叫用户ID
        /// </summary>
        public Guid CallerId { get; set; }

        /// <summary>
        /// 被叫用户ID
        /// </summary>
        public Guid CalleeId { get; set; }

        /// <summary>
        /// 通话状态
        /// </summary>
        public CallState CallState { get; set; }

        /// <summary>
        /// 状态变更原因（可选）
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 状态变更时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}