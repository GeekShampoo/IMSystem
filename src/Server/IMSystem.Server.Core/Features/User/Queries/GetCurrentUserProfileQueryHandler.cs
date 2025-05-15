using AutoMapper;
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.User.Queries;

public class GetCurrentUserProfileQueryHandler : IRequestHandler<GetCurrentUserProfileQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCurrentUserProfileQueryHandler> _logger;

    public GetCurrentUserProfileQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<GetCurrentUserProfileQueryHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        _logger.LogInformation("尝试获取用户ID为 {UserId} 的个人资料 (包含Profile)。", userId);
        var user = await _userRepository.GetByIdWithProfileAsync(userId);


        if (user == null)
        {
            _logger.LogWarning("未找到用户ID为 {UserId} 的用户 (或其Profile)。", userId);
            return Result<UserDto>.Failure("User.NotFound", $"User profile for ID {userId} not found.");
        }

        var userDto = _mapper.Map<UserDto>(user);
        return Result<UserDto>.Success(userDto);
    }
}