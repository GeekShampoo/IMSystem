using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Requests.Messages
{
    /// <summary>
    /// 获取指定会话某序列号之后的消息请求
    /// </summary>
    public class GetMessagesAfterSequenceRequest
    {
        /// <summary>
        /// 接收者ID（用户ID或群组ID）
        /// </summary>
        public Guid RecipientId { get; set; }
        
        /// <summary>
        /// 聊天类型（个人或群组）
        /// </summary>
        public ProtocolChatType ChatType { get; set; }
        
        /// <summary>
        /// 获取此序列号之后的消息
        /// </summary>
        public long AfterSequence { get; set; }
        
        /// <summary>
        /// 最大返回消息数量（可选）
        /// </summary>
        public int? Limit { get; set; }
    }
}