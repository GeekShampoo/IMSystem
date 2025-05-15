using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.Common; // For PagedResult and Result
using MediatR;
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Core.Features.Messages.Queries
{
    public class GetGroupMessagesQuery : IRequest<Result<PagedResult<MessageDto>>>
    {
        /// <summary>
        /// 当前请求历史消息的用户ID (用于权限验证，确保用户是群成员)。
        /// </summary>
        public Guid CurrentUserId { get; }

        /// <summary>
        /// 群组的ID。
        /// </summary>
        public Guid GroupId { get; }

        /// <summary>
        /// The page number to retrieve (1-based).
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The number of messages per page.
        /// </summary>
        public int PageSize { get; }

        public GetGroupMessagesQuery(Guid currentUserId, Guid groupId, int pageNumber = 1, int pageSize = 20)
        {
            if (currentUserId == Guid.Empty)
                throw new ArgumentException("当前用户ID不能为空。", nameof(currentUserId));
            if (groupId == Guid.Empty)
                throw new ArgumentException("群组ID不能为空。", nameof(groupId));
            if (pageNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "页码必须大于零。");
            if (pageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "每页数量必须大于零。");
            if (pageSize > 100) // 示例最大限制
                throw new ArgumentOutOfRangeException(nameof(pageSize), "每页数量不能超过100。");

            CurrentUserId = currentUserId;
            GroupId = groupId;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}