using System;

namespace IMSystem.Protocol.DTOs.Requests.Signaling
{
    /// <summary>
    /// 音视频通话拒绝请求
    /// </summary>
    public class CallRejectRequest
    {
        /// <summary>
        /// 主叫用户ID
        /// </summary>
        public Guid CallerId { get; set; }

        /// <summary>
        /// 被叫用户ID
        /// </summary>
        public Guid CalleeId { get; set; }

        /// <summary>
        /// 通话会话ID
        /// </summary>
        public Guid CallId { get; set; }

        /// <summary>
        /// 拒绝原因
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 拒绝时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}