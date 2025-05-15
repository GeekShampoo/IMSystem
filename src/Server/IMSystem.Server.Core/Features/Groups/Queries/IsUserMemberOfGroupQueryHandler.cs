using MediatR;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // For logging
using System; // For Guid

namespace IMSystem.Server.Core.Features.Group.Queries
{
    public class IsUserMemberOfGroupQueryHandler : IRequestHandler<IsUserMemberOfGroupQuery, Result<bool>>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly ILogger<IsUserMemberOfGroupQueryHandler> _logger;

        public IsUserMemberOfGroupQueryHandler(IGroupRepository groupRepository, ILogger<IsUserMemberOfGroupQueryHandler> logger)
        {
            _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(IsUserMemberOfGroupQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Checking if user {UserId} is a member of group {GroupId}", request.UserId, request.GroupId);
                bool isMember = await _groupRepository.IsUserMemberOfGroupAsync(request.UserId, request.GroupId);
                
                if (isMember)
                {
                    _logger.LogInformation("User {UserId} is a member of group {GroupId}", request.UserId, request.GroupId);
                }
                else
                {
                    _logger.LogInformation("User {UserId} is NOT a member of group {GroupId}", request.UserId, request.GroupId);
                }
                return Result<bool>.Success(isMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking group membership for User {UserId} and Group {GroupId}", request.UserId, request.GroupId);
                return Result<bool>.Failure(new Error("Group.MembershipCheckFailed", $"An error occurred while checking group membership: {ex.Message}"));
            }
        }
    }
}