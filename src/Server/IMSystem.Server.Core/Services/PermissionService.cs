using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Entities; // For GroupMember, Friendship entities
using IMSystem.Server.Domain.Enums;     // For GroupMemberRole, FriendshipStatus enums

namespace IMSystem.Server.Core.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        // private readonly IGroupRepository _groupRepository; // Example if needed

        public PermissionService(
            IGroupMemberRepository groupMemberRepository,
            IFriendshipRepository friendshipRepository
            /*, IGroupRepository groupRepository */)
        {
            _groupMemberRepository = groupMemberRepository ?? throw new ArgumentNullException(nameof(groupMemberRepository));
            _friendshipRepository = friendshipRepository ?? throw new ArgumentNullException(nameof(friendshipRepository));
            // _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        }

        public async Task<bool> IsUserMemberOfGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
        {
            // Using ExistsAsync is efficient for just checking existence.
            // Alternatively, could use:
            // var member = await _groupMemberRepository.GetMemberOrDefaultAsync(groupId, userId, cancellationToken);
            // return member != null;
            // Or the specific method from IGroupMemberRepository if it was added:
            // return await _groupMemberRepository.IsUserMemberOfGroupAsync(groupId, userId, cancellationToken);
            // For now, using the provided ExistsAsync from IGenericRepository as it's clear.
            return await _groupMemberRepository.ExistsAsync(gm => gm.UserId == userId && gm.GroupId == groupId, cancellationToken);
        }

        public async Task<bool> CanUserManageGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
        {
            // GetMemberOrDefaultAsync in IGroupMemberRepository does not take a CancellationToken
            var member = await _groupMemberRepository.GetMemberOrDefaultAsync(groupId, userId);
            return member != null && (member.Role == GroupMemberRole.Owner || member.Role == GroupMemberRole.Admin);
        }

        public async Task<bool> AreUsersFriendsAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default)
        {
            var friendship = await _friendshipRepository.GetFriendshipAsync(userId1, userId2);
            // No, GetFriendshipAsync doesn't take a CancellationToken in the interface I read.
            // And it doesn't filter by status.
            // So, I need to use the FindAsync or check status after getting the entity.
            // The original FindAsync was better here if we need to check status directly in query.
            // Let's revert to FindAsync for this method to include status check, or check status after GetFriendshipAsync.

            // Option 1: Use GetFriendshipAsync and check status (simpler if GetFriendshipAsync is preferred)
            // var friendship = await _friendshipRepository.GetFriendshipAsync(userId1, userId2);
            // return friendship != null && friendship.Status == FriendshipStatus.Accepted;

            // Option 2: Use FindAsync to include status in the query (potentially more efficient DB-side)
            // This matches the original intent more closely.
             var friendshipEntity = await _friendshipRepository.FindAsync(f =>
                 ((f.RequesterId == userId1 && f.AddresseeId == userId2) || (f.RequesterId == userId2 && f.AddresseeId == userId1)) &&
                 f.Status == FriendshipStatus.Accepted,
                 cancellationToken);
            return friendshipEntity != null;
        }

        // Implement other methods if they were defined in the interface and are required now...
    }
}