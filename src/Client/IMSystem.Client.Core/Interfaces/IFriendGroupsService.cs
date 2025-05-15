using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.FriendGroups;
using IMSystem.Protocol.DTOs.Responses.FriendGroups;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for managing friend groups.
    /// </summary>
    public interface IFriendGroupsService
    {
        /// <summary>
        /// Gets all friend groups for the current user.
        /// </summary>
        /// <returns>A result containing a list of friend groups.</returns>
        Task<Result<List<FriendGroupDto>>> GetFriendGroupsAsync();

        /// <summary>
        /// Creates a new friend group.
        /// </summary>
        /// <param name="request">The request containing the details for the new friend group.</param>
        /// <returns>A result containing the created friend group.</returns>
        Task<Result<FriendGroupDto>> CreateFriendGroupAsync(CreateFriendGroupRequest request);

        /// <summary>
        /// Updates the name of an existing friend group.
        /// </summary>
        /// <param name="groupId">The ID of the friend group to update.</param>
        /// <param name="request">The request containing the new name for the friend group.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> UpdateFriendGroupNameAsync(string groupId, UpdateFriendGroupRequest request);

        /// <summary>
        /// Deletes a friend group.
        /// </summary>
        /// <param name="groupId">The ID of the friend group to delete.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> DeleteFriendGroupAsync(string groupId);

        /// <summary>
        /// Adds a friend to a specified group.
        /// </summary>
        /// <param name="groupId">The ID of the group to add the friend to.</param>
        /// <param name="request">The request containing the friend's identifier.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> AddFriendToGroupAsync(string groupId, AddFriendToGroupRequest request);

        /// <summary>
        /// Removes a friend from a specified group.
        /// </summary>
        /// <param name="groupId">The ID of the group to remove the friend from.</param>
        /// <param name="friendUserId">The ID of the friend to remove.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> RemoveFriendFromGroupAsync(string groupId, string friendUserId);

        /// <summary>
        /// Moves a friend from their current group to a target group.
        /// </summary>
        /// <param name="currentGroupId">The ID of the friend's current group.</param>
        /// <param name="friendUserId">The ID of the friend to move.</param>
        /// <param name="targetGroupId">The ID of the target group.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> MoveFriendToGroupAsync(string currentGroupId, string friendUserId, string targetGroupId);

        /// <summary>
        /// Reorders the friend groups.
        /// </summary>
        /// <param name="orderedGroupIds">A list of group IDs in the desired order.</param>
        /// <returns>A result indicating success or failure.</returns>
        Task<Result> ReorderFriendGroupsAsync(List<string> orderedGroupIds);
       /// <summary>
       /// Gets the details of a specific friend group by its ID.
       /// </summary>
       /// <param name="groupId">The ID of the friend group.</param>
       /// <returns>A result containing the friend group details.</returns>
       Task<Result<FriendGroupDto>> GetFriendGroupByIdAsync(string groupId);

       /// <summary>
       /// Moves a friend to the default group.
       /// </summary>
       /// <param name="friendshipId">The ID of the friendship to move.</param>
       /// <returns>A result indicating success or failure.</returns>
       Task<Result> MoveFriendToDefaultGroupAsync(string friendshipId);
   }
}