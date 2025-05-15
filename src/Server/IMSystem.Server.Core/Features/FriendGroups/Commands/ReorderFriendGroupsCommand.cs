using IMSystem.Protocol.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

/// <summary>
/// Command to reorder a user's friend groups.
/// </summary>
public class ReorderFriendGroupsCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the user whose friend groups are being reordered.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// A list of FriendGroup IDs in the desired new order.
    /// The order in this list will determine the new 'Order' property of each group.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one group ID must be provided for reordering.")]
    public List<Guid> OrderedGroupIds { get; }

    public ReorderFriendGroupsCommand(Guid userId, List<Guid> orderedGroupIds)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (orderedGroupIds == null || !orderedGroupIds.Any())
            throw new ArgumentNullException(nameof(orderedGroupIds), "Ordered group IDs list cannot be null or empty.");
        
        // Ensure no duplicate group IDs in the list
        if (orderedGroupIds.Distinct().Count() != orderedGroupIds.Count)
            throw new ArgumentException("Ordered group IDs list cannot contain duplicates.", nameof(orderedGroupIds));

        UserId = userId;
        OrderedGroupIds = orderedGroupIds;
    }
}