using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Requests.Signaling
{
    /// <summary>
    /// SDP 交换请求
    /// </summary>
    public class SdpExchangeRequest
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
        /// SDP 内容
        /// </summary>
        public string Sdp { get; set; }

        /// <summary>
        /// SDP 类型（Offer/Answer）
        /// </summary>
        public SdpType SdpType { get; set; }

        /// <summary>
        /// 交换时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}