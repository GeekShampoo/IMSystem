using System;

namespace IMSystem.Protocol.DTOs.Notifications.Signaling
{
    /// <summary>
    /// SDP 交换通知DTO
    /// </summary>
    public class SdpExchangeNotificationDto
    {
        /// <summary>
        /// 通话ID
        /// </summary>
        public Guid CallId { get; set; }
        
        /// <summary>
        /// 发送者ID
        /// </summary>
        public Guid SenderId { get; set; }
        
        /// <summary>
        /// SDP会话描述
        /// </summary>
        public string Sdp { get; set; } = string.Empty;
        
        /// <summary>
        /// SDP类型 ("offer" 或 "answer")
        /// </summary>
        public string SdpType { get; set; } = string.Empty;
        
        /// <summary>
        /// 交换时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}