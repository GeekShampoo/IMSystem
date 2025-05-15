using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups;
using MediatR;
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Core.Features.Groups.Queries
{
    /// <summary>
    /// 获取当前用户加入的所有群组的查询。
    /// </summary>
    public class GetUserGroupsQuery : IRequest<Result<IEnumerable<GroupDto>>>
    {
        /// <summary>
        /// 发起查询的用户ID。
        /// </summary>
        public Guid UserId { get; set; }

        public GetUserGroupsQuery(Guid userId)
        {
            UserId = userId;
        }
    }
}