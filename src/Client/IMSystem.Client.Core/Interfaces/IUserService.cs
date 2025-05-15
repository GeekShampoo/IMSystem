using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.User;
using IMSystem.Protocol.DTOs.Responses.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles user-related operations.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Gets the current user's profile.
        /// </summary>
        /// <returns>A result containing the user's profile if successful, otherwise an error.</returns>
        Task<Result<UserDto>> GetMyProfileAsync();

        /// <summary>
        /// Updates the current user's profile.
        /// </summary>
        /// <param name="request">The request containing the updated profile information.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> UpdateMyProfileAsync(UpdateUserProfileRequest request);

        /// <summary>
        /// Updates the current user's custom status.
        /// </summary>
        /// <param name="request">The request containing the new custom status.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> UpdateMyCustomStatusAsync(UpdateUserCustomStatusRequest request);

        /// <summary>
        /// Verifies the user's email address.
        /// </summary>
        /// <param name="token">The verification token.</param>
        /// <param name="email">The email address to verify.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> VerifyEmailAsync(string token, string email);

        /// <summary>
        /// Searches for users based on the provided criteria. (Optional)
        /// </summary>
        /// <param name="request">The search criteria.</param>
        /// <returns>A paged result containing user summaries if successful, otherwise an error.</returns>
        Task<Result<PagedResult<UserSummaryDto>>> SearchUsersAsync(SearchUsersRequest request);

        /// <summary>
        /// Retrieves information for a batch of users. (Optional)
        /// </summary>
        /// <param name="request">The request containing the list of user IDs.</param>
        /// <returns>A result containing a list of user DTOs if successful, otherwise an error.</returns>
        Task<Result<List<UserDto>>> BatchGetUsersAsync(BatchGetUsersRequest request);

        /// <summary>
        /// Deactivates the current user's account. (Optional)
        /// </summary>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> DeactivateMyAccountAsync();
        /// <summary>
        /// Retrieves the list of blocked users.
        /// </summary>
        /// <returns>A result containing a list of blocked users if successful, otherwise an error.</returns>
        Task<Result<IEnumerable<BlockedUserDto>>> GetBlockedUsersAsync();

        /// <summary>
        /// Retrieves the public profile of a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose profile is to be retrieved.</param>
        /// <returns>A result containing the user's profile if successful, otherwise an error.</returns>
        Task<Result<UserDto>> GetUserProfileAsync(Guid userId);
/// <summary>
        /// 注册一个新用户。
        /// </summary>
        /// <param name="request">用户注册请求详情。</param>
        /// <returns>一个包含结果的对象，如果成功，则包含已注册用户的DTO；否则包含错误信息。</returns>
        Task<Result<UserDto>> RegisterAsync(RegisterUserRequest request);
    }
}