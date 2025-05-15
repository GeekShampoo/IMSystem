using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.Enums;
using MediatR;
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Core.Features.Messages.Queries
{
    /// <summary>
    /// 获取指定序列号之后的消息查询
    /// </summary>
    public class GetMessagesAfterSequenceQuery : IRequest<Result<IEnumerable<MessageDto>>>
    {
        /// <summary>
        /// 当前查询的用户ID (用于权限验证)
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 接收者ID (用户ID或群组ID)
        /// </summary>
        public Guid RecipientId { get; set; }

        /// <summary>
        /// 聊天类型 (个人或群组)
        /// </summary>
        public ProtocolChatType ChatType { get; set; }

        /// <summary>
        /// 获取此序列号之后的消息
        /// </summary>
        public long AfterSequence { get; set; }

        /// <summary>
        /// 最大返回消息数量
        /// </summary>
        public int Limit { get; set; } = 50;
        
        public GetMessagesAfterSequenceQuery() { }

        public GetMessagesAfterSequenceQuery(Guid userId, Guid recipientId, ProtocolChatType chatType, long afterSequence, int limit = 50)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (recipientId == Guid.Empty)
                throw new ArgumentException("接收者ID不能为空", nameof(recipientId));
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "消息数量限制必须大于0");
            if (limit > 200) // 设置一个合理的上限
                throw new ArgumentOutOfRangeException(nameof(limit), "消息数量限制不能超过200");
            
            UserId = userId;
            RecipientId = recipientId;
            ChatType = chatType;
            AfterSequence = afterSequence;
            Limit = limit;
        }
    }
}