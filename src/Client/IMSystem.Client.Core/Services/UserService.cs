using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.User;
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Protocol.Enums; // Required for ProtocolGender
using System;
using System.Collections.Generic;
using System.Net.Http; // Required for HttpRequestException
using System.Text; // Required for StringBuilder (alternative for query string)
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Services
{
    /// <summary>
    /// Service responsible for user-related operations.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IApiService _apiService;
        private const string BaseApiPath = "api/Users";

        // Private class for API calls that don't return a meaningful body on success
        private class EmptyResponse { }

        public UserService(IApiService apiService)
        {
            _apiService = apiService;
        }

        private async Task<Result<T>> HandleApiResponseAsync<T>(Func<Task<T>> apiCall)
        {
            try
            {
                var response = await apiCall.Invoke();
                return Result<T>.Success(response);
            }
            catch (HttpRequestException ex) // Or a more specific ApiException if IApiService throws it
            {
                // This is a simplified error handling.
                // In a real app, you might use IApiService.HandleApiErrorResponseAsync
                // or IApiService itself would return Result objects.
                return Result<T>.Failure(new Error("ApiError", ex.Message));
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(new Error("UnexpectedError", ex.Message));
            }
        }

        private async Task<Result> HandleApiResponseAsync(Func<Task> apiCall)
        {
            try
            {
                await apiCall.Invoke();
                return Result.Success();
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure(new Error("ApiError", ex.Message));
            }
            catch (Exception ex)
            {
                return Result.Failure(new Error("UnexpectedError", ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<Result<UserDto>> GetMyProfileAsync()
        {
            return await HandleApiResponseAsync(() => _apiService.GetAsync<UserDto>($"{BaseApiPath}/me/profile"));
        }

        /// <inheritdoc />
        public async Task<Result> UpdateMyProfileAsync(UpdateUserProfileRequest request)
        {
            return await HandleApiResponseAsync(() => _apiService.PutAsync($"{BaseApiPath}/me/profile", request));
        }

        /// <inheritdoc />
        public async Task<Result> UpdateMyCustomStatusAsync(UpdateUserCustomStatusRequest request)
        {
            return await HandleApiResponseAsync(() => _apiService.PutAsync($"{BaseApiPath}/me/status", request));
        }

        /// <inheritdoc />
        public async Task<Result> VerifyEmailAsync(string token, string email)
        {
            var result = await HandleApiResponseAsync(() => 
                _apiService.GetAsync<EmptyResponse>(
                    $"/api/Authentication/verify-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}"
                )
            );
            
            return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
        }

        /// <inheritdoc />
        public async Task<Result<PagedResult<UserSummaryDto>>> SearchUsersAsync(SearchUsersRequest request)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                queryParams.Add($"keyword={Uri.EscapeDataString(request.Keyword)}");
            }
            queryParams.Add($"pageNumber={request.PageNumber}");
            queryParams.Add($"pageSize={request.PageSize}");
            if (request.Gender.HasValue)
            {
                queryParams.Add($"gender={(int)request.Gender.Value}");
            }
            var queryString = string.Join("&", queryParams);
            
            return await HandleApiResponseAsync(() => 
                _apiService.GetAsync<PagedResult<UserSummaryDto>>($"{BaseApiPath}/search?{queryString}")
            );
        }

        /// <inheritdoc />
        public async Task<Result<List<UserDto>>> BatchGetUsersAsync(BatchGetUsersRequest request)
        {
            return await HandleApiResponseAsync(() => 
                _apiService.PostAsync<BatchGetUsersRequest, List<UserDto>>($"{BaseApiPath}/batch-get", request)
            );
        }

        /// <inheritdoc />
        public async Task<Result> DeactivateMyAccountAsync()
        {
            return await HandleApiResponseAsync(() => _apiService.DeleteAsync($"{BaseApiPath}/me/account"));
        }
        /// <inheritdoc />
        public async Task<Result<IEnumerable<BlockedUserDto>>> GetBlockedUsersAsync()
        {
            return await HandleApiResponseAsync(() => _apiService.GetAsync<IEnumerable<BlockedUserDto>>($"{BaseApiPath}/blocked"));
        }

        /// <inheritdoc />
        public async Task<Result<UserDto>> GetUserProfileAsync(Guid userId)
        {
            return await HandleApiResponseAsync(() => _apiService.GetAsync<UserDto>($"{BaseApiPath}/{userId}"));
        }
/// <inheritdoc />
        public async Task<Result<UserDto>> RegisterAsync(RegisterUserRequest request)
        {
            return await HandleApiResponseAsync(() =>
                _apiService.PostAsync<RegisterUserRequest, UserDto>($"{BaseApiPath}/register", request)
            );
        }
    }
}