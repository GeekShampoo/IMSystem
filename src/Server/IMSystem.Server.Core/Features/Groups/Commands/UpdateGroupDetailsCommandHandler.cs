using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.Common;
using IMSystem.Server.Domain.Entities; // For Group entity
using IMSystem.Server.Domain.Enums;   // For GroupMemberRole
using IMSystem.Server.Domain.Events.Groups; // For GroupDetailsUpdatedEvent
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging; // For ILogger

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class UpdateGroupDetailsCommandHandler : IRequestHandler<UpdateGroupDetailsCommand, Result>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateGroupDetailsCommandHandler> _logger;
    private readonly IPublisher _publisher; // Added for publishing domain events

    public UpdateGroupDetailsCommandHandler(
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateGroupDetailsCommandHandler> logger,
        IPublisher publisher) // Added IPublisher
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _publisher = publisher; // Added IPublisher
    }

    public async Task<Result> Handle(UpdateGroupDetailsCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdWithMembersAsync(request.GroupId);

        if (group is null)
        {
            _logger.LogWarning("Attempted to update non-existent group {GroupId}", request.GroupId);
            return Result.Failure("Group.NotFound", $"Group with ID {request.GroupId} not found.");
        }

        // 权限检查：只有群主或管理员可以修改群信息
        var member = group.Members?.FirstOrDefault(m => m.UserId == request.UserId);
        if (member is null || (member.Role != GroupMemberRole.Owner && member.Role != GroupMemberRole.Admin))
        {
            _logger.LogWarning("User {UserId} does not have permission to update group {GroupId}. Role: {Role}",
                request.UserId, request.GroupId, member?.Role.ToString() ?? "Not a member");
            return Result.Failure("Group.UpdateDetails.AccessDenied", "You do not have permission to update this group's details.");
        }

        // bool actuallyUpdated = false; // Removed duplicate declaration
        bool actuallyUpdated = false;
        string oldName = group.Name;
        string? oldDescription = group.Description;
        string? oldAvatarUrl = group.AvatarUrl;

        string finalName = group.Name;
        string? finalDescription = group.Description;
        string? finalAvatarUrl = group.AvatarUrl;

        if (request.Name != null && request.Name != oldName)
        {
            // Check for name conflict
            var existingGroupWithName = await _groupRepository.GetByNameAndOwnerAsync(request.Name, group.OwnerId);
            if (existingGroupWithName != null && existingGroupWithName.Id != group.Id)
            {
                _logger.LogWarning("User {OwnerId} attempted to rename group {GroupId} to '{NewName}', but a group with that name already exists.", group.OwnerId, group.Id, request.Name);
                return Result.Failure("Group.NameConflict", $"You already have a group named '{request.Name}'.");
            }
            finalName = request.Name;
            actuallyUpdated = true;
        }

        // If request.Description is provided (even if it's an empty string to clear it), update.
        // If request.Description is null (meaning client didn't send it), don't update.
        if (request.Description != null && request.Description != oldDescription)
        {
            finalDescription = request.Description; // Allows setting to "" to clear
            actuallyUpdated = true;
        }
        
        if (request.AvatarUrl != null && request.AvatarUrl != oldAvatarUrl)
        {
            finalAvatarUrl = request.AvatarUrl; // Allows setting to "" to clear
            actuallyUpdated = true;
        }

        if (actuallyUpdated)
        {
            group.UpdateDetails(finalName, finalDescription, finalAvatarUrl, request.UserId);
        }


        if (!actuallyUpdated)
        {
            _logger.LogInformation("Group {GroupId} details were not changed by user {UserId}.", request.GroupId, request.UserId);
            return Result.Success();
        }

        // _groupRepository.Update(group); // EF Core tracks changes.

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Group {GroupId} details updated by user {UserId}. New Name: '{NewName}', New Description: '{NewDescription}', New Avatar: '{NewAvatarUrl}'",
            request.GroupId, request.UserId, group.Name, group.Description ?? "N/A", group.AvatarUrl ?? "N/A");

        // Publish domain event
        var updatedEvent = new GroupDetailsUpdatedEvent(
            group.Id,
            request.UserId,
            newName: group.Name, // Use the actual new name from the entity
            oldName: oldName,
            newDescription: group.Description, // Actual new description
            oldDescription: oldDescription,
            newAvatarUrl: group.AvatarUrl, // Actual new avatar URL
            oldAvatarUrl: oldAvatarUrl
        );
        // 领域事件统一通过实体 AddDomainEvent 添加，禁止直接 Publish，事件将由 Outbox 机制可靠交付
        group.AddDomainEvent(updatedEvent);

        return Result.Success();
    }
}