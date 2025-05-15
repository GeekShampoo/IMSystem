using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Queries;

public class GetGroupDetailsQueryHandler : IRequestHandler<GetGroupDetailsQuery, Result<GroupDto>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository; // Added
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepository; // 用于获取用户信息

    public GetGroupDetailsQueryHandler(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository, // Added
        IMapper mapper,
        IUserRepository userRepository)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository; // Added
        _mapper = mapper;
        _userRepository = userRepository;
    }

    public async Task<Result<GroupDto>> Handle(GetGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        // 权限校验：仅群组成员可查看群组详情
        var member = await _groupMemberRepository.GetMemberOrDefaultAsync(request.GroupId, request.CurrentUserId);
        if (member == null)
        {
            return Result<GroupDto>.Failure("Group.Forbidden", "您无权查看该群组详情。");
        }

        // 1. Get basic group details (without all members initially)
        var group = await _groupRepository.GetByIdAsync(request.GroupId); // Assuming GetByIdAsync doesn't fetch all members

        if (group is null)
        {
            return Result<GroupDto>.Failure("Group.NotFound", $"Group with ID {request.GroupId} not found.");
        }

        var groupDto = _mapper.Map<GroupDto>(group);

        // 2. Get paged members
        var (members, totalCount) = await _groupMemberRepository.GetMembersByGroupIdPagedAsync(
            request.GroupId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var memberDtos = new List<GroupMemberDto>();
        if (members != null && members.Any())
        {
            memberDtos = members.Select(gm =>
            {
                var memberDto = _mapper.Map<GroupMemberDto>(gm);
                // User details should be included by GetMembersByGroupIdPagedAsync if configured with .Include(u => u.User).ThenInclude(p => p.Profile)
                if (gm.User != null)
                {
                    memberDto.Username = gm.User.Username;
                    memberDto.Nickname = gm.User.Profile?.Nickname;
                    memberDto.AvatarUrl = gm.User.Profile?.AvatarUrl;
                }
                return memberDto;
            }).ToList();
        }
        
        groupDto.Members = PagedResult<GroupMemberDto>.Success(memberDtos, totalCount, request.PageNumber, request.PageSize);
        
        // Optionally, populate CurrentUserRole if needed and if CurrentUserId is part of the request
        // var currentMember = await _groupMemberRepository.GetMemberOrDefaultAsync(request.GroupId, request.CurrentUserId); // Assuming CurrentUserId is available
        // if (currentMember != null) groupDto.CurrentUserRole = currentMember.Role.ToString();


        return Result<GroupDto>.Success(groupDto);
    }
}