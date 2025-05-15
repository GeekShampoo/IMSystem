using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.FriendGroups;
using IMSystem.Protocol.DTOs.Responses.FriendGroups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Services
{
    public class FriendGroupsService : IFriendGroupsService
    {
        private readonly IApiService _apiService;
        private const string BaseApiPath = "api/friend-groups";

        public FriendGroupsService(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public async Task<Result<List<FriendGroupDto>>> GetFriendGroupsAsync()
        {
            try
            {
                var response = await _apiService.GetAsync<List<FriendGroupDto>>(BaseApiPath);
                return Result<List<FriendGroupDto>>.Success(response);
            }
            catch (Exception ex) // Consider a more specific exception type if IApiService throws one
            {
                // Log exception ex here
                return Result<List<FriendGroupDto>>.Failure("ApiError", $"Failed to get friend groups: {ex.Message}");
            }
        }

        public async Task<Result<FriendGroupDto>> CreateFriendGroupAsync(CreateFriendGroupRequest request)
        {
            if (request == null)
                return Result<FriendGroupDto>.Failure("ValidationFailed", "Request cannot be null.");
            try
            {
                var response = await _apiService.PostAsync<CreateFriendGroupRequest, FriendGroupDto>(BaseApiPath, request);
                return Result<FriendGroupDto>.Success(response);
            }
            catch (Exception ex)
            {
                // Log exception ex here
                return Result<FriendGroupDto>.Failure("ApiError", $"Failed to create friend group: {ex.Message}");
            }
        }

        public async Task<Result> UpdateFriendGroupNameAsync(string groupId, UpdateFriendGroupRequest request)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return Result.Failure("ValidationFailed", "Group ID cannot be empty.");
            if (request == null)
                return Result.Failure("ValidationFailed", "Request cannot be null.");
            try
            {
                // IApiService.PutAsync<TRequest> returns Task, not Task<TResponse>
                await _apiService.PutAsync($"{BaseApiPath}/{groupId}", request);
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Log exception ex here
                return Result.Failure("ApiError", $"Failed to update friend group name: {ex.Message}");
            }
        }

        public async Task<Result> DeleteFriendGroupAsync(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return Result.Failure("ValidationFailed", "Group ID cannot be empty.");
            try
            {
                await _apiService.DeleteAsync($"{BaseApiPath}/{groupId}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Log exception ex here
                return Result.Failure("ApiError", $"Failed to delete friend group: {ex.Message}");
            }
        }

        public async Task<Result> AddFriendToGroupAsync(string groupId, AddFriendToGroupRequest request)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return Result.Failure("ValidationFailed", "Group ID cannot be empty.");
            if (request == null || request.FriendshipId == Guid.Empty)
                return Result.Failure("ValidationFailed", "Request or FriendshipId in request cannot be empty.");
            try
            {
                // IApiService.PostAsync<TRequest> returns Task
                await _apiService.PostAsync($"{BaseApiPath}/{groupId}/friends", request);
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Log exception ex here
                return Result.Failure("ApiError", $"Failed to add friend to group: {ex.Message}");
            }
        }

        public async Task<Result> RemoveFriendFromGroupAsync(string groupId, string friendUserId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return Result.Failure("ValidationFailed", "Group ID cannot be empty.");
            if (string.IsNullOrWhiteSpace(friendUserId))
                return Result.Failure("ValidationFailed", "Friend User ID cannot be empty.");
            try
            {
                await _apiService.DeleteAsync($"{BaseApiPath}/{groupId}/friends/{friendUserId}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Log exception ex here
                return Result.Failure("ApiError", $"Failed to remove friend from group: {ex.Message}");
            }
        }

        public async Task<Result> MoveFriendToGroupAsync(string currentGroupId, string friendUserId, string targetGroupId)
        {
            if (string.IsNullOrWhiteSpace(currentGroupId))
                return Result.Failure("ValidationFailed", "Current Group ID cannot be empty.");
            if (string.IsNullOrWhiteSpace(friendUserId))
                return Result.Failure("ValidationFailed", "Friend User ID (as FriendshipId string) cannot be empty.");
            if (string.IsNullOrWhiteSpace(targetGroupId))
                return Result.Failure("ValidationFailed", "Target Group ID cannot be empty.");

            if (currentGroupId == targetGroupId)
                return Result.Success();

            var removeResult = await RemoveFriendFromGroupAsync(currentGroupId, friendUserId);
            if (!removeResult.IsSuccess)
            {
                // Error already formatted by RemoveFriendFromGroupAsync
                return removeResult;
            }
            
            if (!Guid.TryParse(friendUserId, out var friendshipIdGuid))
            {
                return Result.Failure("ValidationFailed", $"Friend User ID '{friendUserId}' is not a valid GUID for FriendshipId.");
            }

            var addRequest = new AddFriendToGroupRequest { FriendshipId = friendshipIdGuid };
            var addResult = await AddFriendToGroupAsync(targetGroupId, addRequest);
            if (!addResult.IsSuccess)
            {
                // Attempt to roll back
                var rollbackRequest = new AddFriendToGroupRequest { FriendshipId = friendshipIdGuid };
                await AddFriendToGroupAsync(currentGroupId, rollbackRequest); // Best effort rollback
                // Error already formatted by AddFriendToGroupAsync
                return addResult;
            }

            return Result.Success();
        }

        public async Task<Result> ReorderFriendGroupsAsync(List<string> orderedGroupIds)
        {
            if (orderedGroupIds == null || !orderedGroupIds.Any())
                return Result.Failure("ValidationFailed", "Ordered group IDs list cannot be null or empty.");

            var guidList = new List<Guid>();
            foreach (var idStr in orderedGroupIds)
            {
                if (Guid.TryParse(idStr, out var guid))
                {
                    guidList.Add(guid);
                }
                else
                {
                    return Result.Failure("ValidationFailed", $"Invalid GUID format in ordered list: '{idStr}'.");
                }
            }
            try
            {
                // Assuming PostAsync<TRequest> for reorder as it's a list of Guids and likely doesn't return complex type
                await _apiService.PostAsync($"{BaseApiPath}/reorder", guidList);
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Log exception ex here
                return Result.Failure("ApiError", $"Failed to reorder friend groups: {ex.Message}");
            }
        }
       /// <summary>
       /// Gets the details of a specific friend group by its ID.
       /// </summary>
       /// <param name="groupId">The ID of the friend group.</param>
       /// <returns>A result containing the friend group details.</returns>
       public async Task<Result<FriendGroupDto>> GetFriendGroupByIdAsync(string groupId)
       {
           if (string.IsNullOrWhiteSpace(groupId))
               return Result<FriendGroupDto>.Failure("ValidationFailed", "Group ID cannot be empty.");
           try
           {
               var response = await _apiService.GetAsync<FriendGroupDto>($"{BaseApiPath}/{groupId}");
               return Result<FriendGroupDto>.Success(response);
           }
           catch (Exception ex)
           {
               // Log exception ex here
               return Result<FriendGroupDto>.Failure("ApiError", $"Failed to get friend group details: {ex.Message}");
           }
       }

       /// <summary>
       /// Moves a friend to the default group.
       /// </summary>
       /// <param name="friendshipId">The ID of the friendship to move.</param>
       /// <returns>A result indicating success or failure.</returns>
       public async Task<Result> MoveFriendToDefaultGroupAsync(string friendshipId)
       {
           if (string.IsNullOrWhiteSpace(friendshipId))
               return Result.Failure("ValidationFailed", "Friendship ID cannot be empty.");
           try
           {
               await _apiService.PostAsync($"api/friend-groups/friends/{friendshipId}/move-to-default", new {});
               return Result.Success();
           }
           catch (Exception ex)
           {
               // Log exception ex here
               return Result.Failure("ApiError", $"Failed to move friend to default group: {ex.Message}");
           }
       }
   }
}