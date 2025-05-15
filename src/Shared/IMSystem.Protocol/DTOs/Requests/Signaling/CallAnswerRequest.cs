using System;

namespace IMSystem.Protocol.DTOs.Requests.Signaling
{
    /// <summary>
    /// 音视频通话应答请求
    /// </summary>
    public class CallAnswerRequest
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
        /// 是否同意通话
        /// </summary>
        public bool Accepted { get; set; }

        /// <summary>
        /// 应答时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}