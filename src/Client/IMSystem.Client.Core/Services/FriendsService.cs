using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.Friends;
using IMSystem.Protocol.DTOs.Responses.Friends;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Services
{
    /// <summary>
    /// Implements the <see cref="IFriendsService"/> to interact with friend-related API endpoints.
    /// </summary>
    public class FriendsService : IFriendsService
    {
        private readonly IApiService _apiService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FriendsService"/> class.
        /// </summary>
        /// <param name="apiService">The API service to make HTTP requests.</param>
        public FriendsService(IApiService apiService)
        {
            _apiService = apiService;
        }

/// <summary>
        /// Handles API responses by wrapping the call in a try-catch block and returning a standardized result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="apiCall">The API call to execute.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the standardized result.</returns>
        private async Task<Result<T>> HandleApiResponseAsync<T>(Func<Task<T>> apiCall)
        {
            try
            {
                var response = await apiCall();
                return Result<T>.Success(response);
            }
            catch (Exception ex)
            {
                // Log exception ex here if logging is available
                return Result<T>.Failure("ApiError", $"An error occurred while processing the API call: {ex.Message}");
            }
        }
        

        /// <inheritdoc />
        private class SendFriendRequestApiResponse
        {
            public Guid FriendshipId { get; set; }
        }

        public async Task<Result<Guid>> SendFriendRequestAsync(SendFriendRequestRequest request)
        {
            var response = await _apiService.PostAsync<SendFriendRequestRequest, SendFriendRequestApiResponse>("api/Friends/requests", request);
            return Result<Guid>.Success(response.FriendshipId);
        }

        /// <inheritdoc />
        public async Task<Result<PagedResult<FriendRequestDto>>> GetPendingFriendRequestsAsync(int pageNumber, int pageSize)
        {
            // GetAsync<T> returns T
            var pagedResult = await _apiService.GetAsync<PagedResult<FriendRequestDto>>($"api/Friends/requests/pending?pageNumber={pageNumber}&pageSize={pageSize}");
            return Result<PagedResult<FriendRequestDto>>.Success(pagedResult);
        }

        /// <inheritdoc />
        public async Task<Result> AcceptFriendRequestAsync(string requestId)
        {
            // PutAsync<TRequest> returns void (Task)
            await _apiService.PutAsync<object>($"api/Friends/requests/{requestId}/accept", null);
            return Result.Success(); // Use non-generic Result.Success()
        }

        /// <inheritdoc />
        public async Task<Result> DeclineFriendRequestAsync(string requestId)
        {
            // PutAsync<TRequest> returns void (Task)
            await _apiService.PutAsync<object>($"api/Friends/requests/{requestId}/decline", null);
            return Result.Success(); // Use non-generic Result.Success()
        }

        /// <inheritdoc />
        public async Task<Result<PagedResult<FriendDto>>> GetFriendsAsync(int pageNumber = 1, int pageSize = 20)
        {
            return await HandleApiResponseAsync(() => _apiService.GetAsync<PagedResult<FriendDto>>($"api/Friends?pageNumber={pageNumber}&pageSize={pageSize}"));
        }

        /// <inheritdoc />
        public async Task<Result<FriendDto>> GetFriendshipDetailsAsync(string friendshipId)
        {
            return await HandleApiResponseAsync(() => _apiService.GetAsync<FriendDto>($"api/Friends/requests/{friendshipId}"));
        }

        /// <inheritdoc />
        public async Task<Result> RemoveFriendAsync(string friendUserId)
        {
            // DeleteAsync returns void (Task)
            await _apiService.DeleteAsync($"api/Friends/{friendUserId}");
            return Result.Success(); // Non-generic success
        }

        /// <inheritdoc />
        public async Task<Result> BlockFriendAsync(string friendUserId)
        {
            // PostAsync<TRequest> returns void (Task)
            await _apiService.PostAsync<object>($"api/Friends/{friendUserId}/block", null);
            return Result.Success(); // Use non-generic Result.Success()
        }

        /// <inheritdoc />
        public async Task<Result> UnblockFriendAsync(string friendUserId)
        {
            // PostAsync<TRequest> returns void (Task)
            await _apiService.PostAsync<object>($"api/Friends/{friendUserId}/unblock", null);
            return Result.Success(); // Use non-generic Result.Success()
        }

        /// <inheritdoc />
        public async Task<Result> SetFriendRemarkAsync(string friendUserId, SetFriendRemarkRequest request)
        {
            // PutAsync(string, TRequest) returns void (Task)
            await _apiService.PutAsync($"api/Friends/{friendUserId}/remark", request);
            return Result.Success(); // Use non-generic Result.Success()
        }
    }
}