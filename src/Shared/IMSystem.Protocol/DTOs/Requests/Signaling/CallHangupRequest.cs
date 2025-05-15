using System;

namespace IMSystem.Protocol.DTOs.Requests.Signaling
{
    /// <summary>
    /// 音视频通话挂断请求
    /// </summary>
    public class CallHangupRequest
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
        /// 挂断原因（可选）
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 挂断时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}