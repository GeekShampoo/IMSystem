using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Protocol.Enums;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.User.Queries
{
    public class SearchUsersQuery : IRequest<Result<PagedResult<UserSummaryDto>>>
    {
        public string? Keyword { get; }
        public ProtocolGender? Gender { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public Guid CurrentUserId { get; }

        public SearchUsersQuery(Guid currentUserId, string? keyword, ProtocolGender? gender, int pageNumber = 1, int pageSize = 20)
        {
            if (currentUserId == Guid.Empty)
            {
                throw new ArgumentException("当前用户ID不能为空。", nameof(currentUserId));
            }
            if (pageNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "页码必须大于等于1。");
            }
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "每页数量必须大于等于1。");
            }

            CurrentUserId = currentUserId;
            Keyword = keyword?.Trim();
            Gender = gender;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}