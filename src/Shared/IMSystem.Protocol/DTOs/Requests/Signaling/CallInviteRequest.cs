using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Requests.Signaling
{
    /// <summary>
    /// 发起音视频通话邀请请求
    /// </summary>
    public class CallInviteRequest
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
        /// 通话类型（音频/视频）
        /// </summary>
        public CallType CallType { get; set; }

        /// <summary>
        /// 发起时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}