using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.User.Queries
{
    public class BatchGetUsersQueryHandler : IRequestHandler<BatchGetUsersQuery, Result<List<UserSummaryDto>>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<BatchGetUsersQueryHandler> _logger;

        public BatchGetUsersQueryHandler(IUserRepository userRepository, IMapper mapper, ILogger<BatchGetUsersQueryHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<List<UserSummaryDto>>> Handle(BatchGetUsersQuery request, CancellationToken cancellationToken)
        {
            if (request.UserExternalIds == null || !request.UserExternalIds.Any())
            {
                _logger.LogWarning("BatchGetUsersQueryHandler received empty or null UserExternalIds list.");
                return Result<List<UserSummaryDto>>.Success(new List<UserSummaryDto>());
            }

            try
            {
                var users = await _userRepository.GetUsersByExternalIdsAsync(request.UserExternalIds);

                if (users == null || !users.Any())
                {
                    _logger.LogInformation("No users found for the provided external IDs.");
                    return Result<List<UserSummaryDto>>.Success(new List<UserSummaryDto>());
                }

                var userSummaries = _mapper.Map<List<UserSummaryDto>>(users);
                

                _logger.LogInformation("Successfully retrieved {Count} user summaries.", userSummaries.Count);
                return Result<List<UserSummaryDto>>.Success(userSummaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while handling BatchGetUsersQuery for {IdCount} IDs.", request.UserExternalIds.Count);
                return Result<List<UserSummaryDto>>.Failure("User.BatchGet.Error", $"An error occurred while retrieving user information: {ex.Message}");
            }
        }
    }
}