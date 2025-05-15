using System.Collections.Generic;

namespace IMSystem.Protocol.DTOs.Responses.Messages
{
    /// <summary>
    /// 获取指定会话某序列号之后的消息响应
    /// </summary>
    public class GetMessagesAfterSequenceResponse
    {
        public List<IMSystem.Protocol.DTOs.Messages.MessageDto> Messages { get; set; }
        public long MaxSequenceNumber { get; set; }
    }
}