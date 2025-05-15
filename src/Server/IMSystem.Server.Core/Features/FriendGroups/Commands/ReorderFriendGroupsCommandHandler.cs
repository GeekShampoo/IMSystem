using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic; // Required for List
using IMSystem.Server.Domain.Events.FriendGroups; // For FriendGroupsReorderedEvent

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

public class ReorderFriendGroupsCommandHandler : IRequestHandler<ReorderFriendGroupsCommand, Result>
{
    private readonly IFriendGroupRepository _friendGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReorderFriendGroupsCommandHandler> _logger;
    private readonly IPublisher _publisher; // Added for publishing domain events

    public ReorderFriendGroupsCommandHandler(
        IFriendGroupRepository friendGroupRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReorderFriendGroupsCommandHandler> logger,
        IPublisher publisher) // Added IPublisher
    {
        _friendGroupRepository = friendGroupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _publisher = publisher; // Added IPublisher
    }

    public async Task<Result> Handle(ReorderFriendGroupsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempting to reorder friend groups. Ordered IDs: {OrderedGroupIds}",
            request.UserId, string.Join(", ", request.OrderedGroupIds));

        var userGroups = (await _friendGroupRepository.GetByUserIdAsync(request.UserId)).ToList();

        if (userGroups == null || !userGroups.Any())
        {
            _logger.LogWarning("User {UserId} has no friend groups to reorder.", request.UserId);
            // If the provided list is also empty, this could be considered a success (no-op).
            // If provided list is not empty but user has no groups, it's an inconsistency.
            return Result.Failure("FriendGroup.NoGroups", "No friend groups found for the user to reorder.");
        }

        // Validate that all provided IDs belong to the user and that all user's groups are present (unless some are fixed like 'Default')
        var userGroupIds = userGroups.Select(g => g.Id).ToList();
        var providedGroupIdsSet = request.OrderedGroupIds.ToHashSet();

        // Check if all provided IDs are valid groups of the user
        if (!providedGroupIdsSet.IsSubsetOf(userGroupIds))
        {
            var invalidIds = request.OrderedGroupIds.Where(id => !userGroupIds.Contains(id));
            _logger.LogWarning("User {UserId} provided invalid group IDs for reordering: {InvalidIds}", request.UserId, string.Join(", ", invalidIds));
            return Result.Failure("FriendGroup.InvalidIds", "One or more provided group IDs are invalid or do not belong to the user.");
        }
        
        // Check if the number of provided IDs matches the number of reorderable groups for the user.
        // This assumes all non-default groups are reorderable and must be included in the list.
        // A more flexible approach might allow reordering a subset, but that complicates order management.
        // For now, assume the client sends the complete list of non-default groups in the new order.
        // The default group's order is fixed and should not be part of this reordering typically.
        var reorderableUserGroups = userGroups.Where(g => !g.IsDefault).ToList();
        if (request.OrderedGroupIds.Count != reorderableUserGroups.Count)
        {
             _logger.LogWarning("User {UserId} reorder request count mismatch. Expected {ExpectedCount}, Got {ActualCount}. Non-default groups must all be included.",
                request.UserId, reorderableUserGroups.Count, request.OrderedGroupIds.Count);
            return Result.Failure("FriendGroup.CountMismatch", "The list of group IDs for reordering must include all your non-default groups.");
        }
         if (!providedGroupIdsSet.SetEquals(reorderableUserGroups.Select(g=>g.Id).ToHashSet()))
        {
            _logger.LogWarning("User {UserId} reorder request ID set mismatch. The provided IDs do not exactly match the user's reorderable groups.", request.UserId);
            return Result.Failure("FriendGroup.IdSetMismatch", "The provided group IDs do not match your reorderable groups.");
        }


        try
        {
            int currentOrder = 0; // Start ordering from 0 or 1 based on preference
            // The default group (if exists) usually has a fixed order (e.g., 0 or highest).
            // We are reordering non-default groups.
            // Let's assume non-default groups start their order after the default group.
            // If default group has order 0, custom groups start from 1.
            // For simplicity, let's re-assign orders from a base value for non-default groups.
            
            var defaultGroup = userGroups.FirstOrDefault(g => g.IsDefault);
            int orderStartIndex = Constants.FriendGroupConstants.DefaultGroupOrder + 1; // Custom groups start after default

            foreach (var groupIdInOrder in request.OrderedGroupIds)
            {
                var groupToUpdate = reorderableUserGroups.FirstOrDefault(g => g.Id == groupIdInOrder);
                if (groupToUpdate != null) // Should always be found due to prior checks
                {
                    // Check if it's the default group - it shouldn't be in OrderedGroupIds if we filter reorderableUserGroups correctly
                    if (groupToUpdate.IsDefault)
                    {
                         _logger.LogWarning("Attempted to reorder the default group {GroupId} for user {UserId}. This should not happen.", groupToUpdate.Id, request.UserId);
                        continue; // Skip default group, its order is fixed.
                    }
                    groupToUpdate.UpdateDetails(groupToUpdate.Name, orderStartIndex, request.UserId); // Update order, keep name
                    orderStartIndex++;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully reordered friend groups for user {UserId}.", request.UserId);

            // Prepare data for the event
            var reorderedGroupData = reorderableUserGroups
                .Where(g => request.OrderedGroupIds.Contains(g.Id)) // Ensure we only include groups that were part of the reorder
                .Select(g => (g.Id, g.Order))
                .ToList();
            
            if (reorderedGroupData.Any())
            {
                var reorderedEvent = new FriendGroupsReorderedEvent(request.UserId, reorderedGroupData);
                // 领域事件无挂载实体时，直接通过 OutboxRepository 持久化，禁止直接 Publish，事件将由 Outbox 机制可靠交付
                var eventType = reorderedEvent.GetType().AssemblyQualifiedName ?? reorderedEvent.GetType().FullName ?? reorderedEvent.GetType().Name;
                var eventPayload = System.Text.Json.JsonSerializer.Serialize(reorderedEvent, reorderedEvent.GetType());
                var outboxMessage = new IMSystem.Server.Domain.Entities.OutboxMessage(eventType, eventPayload, DateTime.UtcNow);
                await _unitOfWork.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                _logger.LogInformation("OutboxMessage for FriendGroupsReorderedEvent added for User {UserId}.", request.UserId);
            }
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering friend groups for user {UserId}.", request.UserId);
            return Result.Failure("FriendGroup.ReorderError", "An error occurred while reordering friend groups.");
        }
    }
}