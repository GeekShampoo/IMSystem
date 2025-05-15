using System;

namespace IMSystem.Protocol.DTOs.Notifications.Signaling
{
    /// <summary>
    /// ICE候选交换通知DTO
    /// </summary>
    public class IceCandidateNotificationDto
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
        /// ICE候选字符串
        /// </summary>
        public string Candidate { get; set; } = string.Empty;
        
        /// <summary>
        /// SDP媒体标识符
        /// </summary>
        public string SdpMid { get; set; } = string.Empty;
        
        /// <summary>
        /// SDP媒体行索引
        /// </summary>
        public int SdpMLineIndex { get; set; }
        
        /// <summary>
        /// 交换时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}