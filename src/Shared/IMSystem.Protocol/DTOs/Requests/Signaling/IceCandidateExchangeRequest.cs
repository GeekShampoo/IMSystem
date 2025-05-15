using System;

namespace IMSystem.Protocol.DTOs.Requests.Signaling
{
    /// <summary>
    /// ICE 候选交换请求
    /// </summary>
    public class IceCandidateExchangeRequest
    {
        /// <summary>
        /// 通话会话ID
        /// </summary>
        public Guid CallId { get; set; }

        /// <summary>
        /// 发送方用户ID
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// 接收方用户ID
        /// </summary>
        public Guid ReceiverId { get; set; }

        /// <summary>
        /// ICE 候选字符串
        /// </summary>
        public string Candidate { get; set; }

        /// <summary>
        /// SDP Mid
        /// </summary>
        public string SdpMid { get; set; }

        /// <summary>
        /// SDP MLine 索引
        /// </summary>
        public int SdpMLineIndex { get; set; }

        /// <summary>
        /// 交换时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}