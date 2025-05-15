using System;
using System.Threading.Tasks;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups;
using IMSystem.Protocol.DTOs.Requests.Groups;

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for group-related services.
    /// </summary>
    public interface IGroupService
    {
        /// <summary>
        /// Gets the groups the current user has joined, with pagination.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The number of groups per page.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged list of groups.</returns>
        Task<Result<PagedResult<GroupDto>>> GetUserGroupsAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Gets the details of a specific group, including its members with pagination.
        /// </summary>
        /// <param name="groupId">The ID of the group to retrieve details for.</param>
        /// <param name="membersPageNumber">The page number for group members.</param>
        /// <param name="membersPageSize">The number of members per page.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the group details.</returns>
        Task<Result<GroupDto>> GetGroupDetailsAsync(Guid groupId, int membersPageNumber, int membersPageSize);
        /// <summary>
        /// Creates a new group.
        /// </summary>
        /// <param name="request">The request containing group creation details.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the ID of the created group.</returns>
        Task<Result<Guid>> CreateGroupAsync(CreateGroupRequest request);

        /// <summary>
        /// Updates the details of an existing group.
        /// </summary>
        /// <param name="groupId">The ID of the group to update.</param>
        /// <param name="request">The request containing updated group details.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<Result> UpdateGroupDetailsAsync(Guid groupId, UpdateGroupDetailsRequest request);
        /// <summary>
        /// Accepts a group invitation.
        /// </summary>
        Task<Result> AcceptGroupInvitationAsync(Guid invitationId);

        /// <summary>
        /// Rejects a group invitation.
        /// </summary>
        Task<Result> RejectGroupInvitationAsync(Guid invitationId);

        /// <summary>
        /// Gets pending group invitations.
        /// </summary>
        Task<Result<IEnumerable<GroupInvitationDto>>> GetPendingGroupInvitationsAsync();

        /// <summary>
        /// Invites a user to a group.
        /// </summary>
        Task<Result<Guid>> InviteUserToGroupAsync(Guid groupId, InviteUserToGroupRequest request);

        /// <summary>
        /// Leaves a group.
        /// </summary>
        Task<Result> LeaveGroupAsync(Guid groupId);

        /// <summary>
        /// Transfers group ownership.
        /// </summary>
        Task<Result> TransferGroupOwnershipAsync(Guid groupId, TransferGroupOwnershipRequest request);

        /// <summary>
        /// Promotes a member to admin.
        /// </summary>
        Task<Result> PromoteMemberToAdminAsync(Guid groupId, Guid memberUserId);

        /// <summary>
        /// Demotes an admin to member.
        /// </summary>
        Task<Result> DemoteAdminToMemberAsync(Guid groupId, Guid memberUserId);

        /// <summary>
        /// Disbands a group.
        /// </summary>
        Task<Result> DisbandGroupAsync(Guid groupId);

        /// <summary>
        /// Cancels a group invitation.
        /// </summary>
        Task<Result> CancelGroupInvitationAsync(Guid invitationId);

        /// <summary>
        /// Gets sent group invitations.
        /// </summary>
        Task<Result<IEnumerable<GroupInvitationDto>>> GetSentGroupInvitationsAsync(Guid groupId);

        /// <summary>
        /// Kicks a group member.
        /// </summary>
        Task<Result> KickGroupMemberAsync(Guid groupId, Guid memberUserId);

        /// <summary>
        /// Sets a group announcement.
        /// </summary>
        Task<Result> SetGroupAnnouncementAsync(Guid groupId, SetGroupAnnouncementRequest request);
    }
}