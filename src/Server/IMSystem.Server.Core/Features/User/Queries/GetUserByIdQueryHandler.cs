using AutoMapper;
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.User.Queries
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto?>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUserByIdQueryHandler> _logger;

        public GetUserByIdQueryHandler(IUserRepository userRepository, IMapper mapper, ILogger<GetUserByIdQueryHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<UserDto?>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to retrieve user with ID: {UserId}", request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId);

            if (user == null)
            {
                _logger.LogWarning("User with ID: {UserId} not found.", request.UserId);
                return Result<UserDto?>.Failure("User.NotFound", $"User with ID: {request.UserId} not found.");
            }

            _logger.LogInformation("Successfully retrieved user with ID: {UserId}. Mapping to UserDto.", request.UserId);
            return Result<UserDto?>.Success(_mapper.Map<UserDto>(user));
        }
    }
}