using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.Friends;
using IMSystem.Protocol.DTOs.Responses.Friends;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles friend-related operations.
    /// </summary>
    public interface IFriendsService
    {
        /// <summary>
        /// Sends a friend request to another user.
        /// </summary>
        /// <param name="request">The send friend request details.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation, including the created friend request if successful.</returns>
        Task<Result<Guid>> SendFriendRequestAsync(SendFriendRequestRequest request);

        /// <summary>
        /// Gets the pending friend requests for the current user.
        /// </summary>
        /// <param name="pageNumber">The page number for pagination.</param>
        /// <param name="pageSize">The page size for pagination.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the paged result of pending friend requests.</returns>
        Task<Result<PagedResult<FriendRequestDto>>> GetPendingFriendRequestsAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Accepts a friend request.
        /// </summary>
        /// <param name="requestId">The ID of the friend request to accept.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> AcceptFriendRequestAsync(string requestId);

        /// <summary>
        /// Declines a friend request.
        /// </summary>
        /// <param name="requestId">The ID of the friend request to decline.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> DeclineFriendRequestAsync(string requestId);

        /// <summary>
        /// Gets the list of friends for the current user with pagination.
        /// </summary>
        /// <param name="pageNumber">The page number for pagination.</param>
        /// <param name="pageSize">The page size for pagination.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the paged result of friends.</returns>
        Task<Result<PagedResult<FriendDto>>> GetFriendsAsync(int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Gets the details of a specific friendship.
        /// </summary>
        /// <param name="friendshipId">The ID of the friendship.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the friendship details.</returns>
        Task<Result<FriendDto>> GetFriendshipDetailsAsync(string friendshipId);

        /// <summary>
        /// Removes a friend.
        /// </summary>
        /// <param name="friendUserId">The user ID of the friend to remove.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> RemoveFriendAsync(string friendUserId);

        /// <summary>
        /// Blocks a friend.
        /// </summary>
        /// <param name="friendUserId">The user ID of the friend to block.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> BlockFriendAsync(string friendUserId);

        /// <summary>
        /// Unblocks a friend.
        /// </summary>
        /// <param name="friendUserId">The user ID of the friend to unblock.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> UnblockFriendAsync(string friendUserId);

        /// <summary>
        /// Sets a remark for a friend.
        /// </summary>
        /// <param name="friendUserId">The user ID of the friend.</param>
        /// <param name="request">The request containing the new remark.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> SetFriendRemarkAsync(string friendUserId, SetFriendRemarkRequest request);
    }
}