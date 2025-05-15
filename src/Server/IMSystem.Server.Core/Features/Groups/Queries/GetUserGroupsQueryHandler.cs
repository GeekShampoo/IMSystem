using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.Queries
{
    /// <summary>
    /// 处理获取用户群组列表查询的处理器。
    /// </summary>
    public class GetUserGroupsQueryHandler : IRequestHandler<GetUserGroupsQuery, Result<IEnumerable<GroupDto>>>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IMapper _mapper;

        public GetUserGroupsQueryHandler(IGroupRepository groupRepository, IMapper mapper)
        {
            _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<Result<IEnumerable<GroupDto>>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return Result<IEnumerable<GroupDto>>.Failure("User.InvalidId", "用户ID不能为空。");
            }

            var groups = await _groupRepository.GetUserGroupsAsync(request.UserId);

            if (groups == null || !groups.Any())
            {
                return Result<IEnumerable<GroupDto>>.Success(new List<GroupDto>()); // 或者返回一个表示未找到的特定结果
            }

            var groupDtos = _mapper.Map<IEnumerable<GroupDto>>(groups);
            return Result<IEnumerable<GroupDto>>.Success(groupDtos);
        }
    }
}