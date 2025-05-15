using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.Common; // For PagedResult and Result
using MediatR;
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Core.Features.Messages.Queries
{
    public class GetUserMessagesQuery : IRequest<Result<PagedResult<MessageDto>>>
    {
        /// <summary>
        /// 当前请求历史消息的用户ID。
        /// </summary>
        public Guid CurrentUserId { get; }

        /// <summary>
        /// 聊天对象的ID。
        /// </summary>
        public Guid OtherUserId { get; }

        /// <summary>
        /// The page number to retrieve (1-based).
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The number of messages per page.
        /// </summary>
        public int PageSize { get; }

        public GetUserMessagesQuery(Guid currentUserId, Guid otherUserId, int pageNumber = 1, int pageSize = 20)
        {
            if (currentUserId == Guid.Empty)
                throw new ArgumentException("当前用户ID不能为空。", nameof(currentUserId));
            if (otherUserId == Guid.Empty)
                throw new ArgumentException("聊天对象ID不能为空。", nameof(otherUserId));
            if (pageNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "页码必须大于零。");
            if (pageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "每页数量必须大于零。");
            if (pageSize > 100) // 示例最大限制
                throw new ArgumentOutOfRangeException(nameof(pageSize), "每页数量不能超过100。");

            CurrentUserId = currentUserId;
            OtherUserId = otherUserId;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}