using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For Group, GroupMember entities
using IMSystem.Server.Domain.Enums;   // For GroupMemberRole
using IMSystem.Protocol.Common;
using MediatR;
using System;
using IMSystem.Server.Domain.Events.Groups; // Added for GroupCreatedEvent
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Exceptions; // For potential EntityNotFoundException if checking CreatorUser

using IMSystem.Server.Core.Common;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Result<Guid>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository; // To verify creator exists
    private readonly IUnitOfWork _unitOfWork;

    public CreateGroupCommandHandler(
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Guid>> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        // 1. 验证创建者是否存在
        var creator = await _userRepository.GetByIdAsync(request.CreatorUserId);
        if (creator is null)
        {
            return Result<Guid>.Failure(UserErrors.NotFound, UserErrors.UserNotFoundDescription(request.CreatorUserId));
        }

        // 2. 检查创建者是否已存在同名群组 (可选，取决于业务需求)
        var existingGroup = await _groupRepository.GetByNameAndOwnerAsync(request.Name, request.CreatorUserId);
        if (existingGroup is not null)
        {
            return Result<Guid>.Failure("Group.NameConflict", GroupErrors.GroupAlreadyExistsDescription(request.Name, request.CreatorUserId));
        }

        // 3. 创建 Group 实体
        // The Group constructor sets CreatedBy and OwnerId to creatorId.
        var group = new Domain.Entities.Group(
            name: request.Name,
            creatorId: request.CreatorUserId,
            description: request.Description,
            avatarUrl: request.AvatarUrl
        );

        // 4. 将创建者添加为群主 (GroupMember)
        // The GroupMember constructor sets CreatedBy to actorId (or userId if actorId is null).
        // Here, the creator is also the actor adding themselves as owner.
        var ownerMember = new GroupMember(
            groupId: group.Id, // The Id is generated in BaseEntity constructor
            userId: request.CreatorUserId,
            role: GroupMemberRole.Owner,
            actorId: request.CreatorUserId
        );

        // 5. 持久化
        await _groupRepository.AddAsync(group);
        await _groupRepository.AddGroupMemberAsync(ownerMember); // Use the new method in IGroupRepository

        // Add domain event before saving changes
        var groupCreatedEvent = new GroupCreatedEvent(group.Id, group.Name, group.OwnerId);
        group.AddDomainEvent(groupCreatedEvent);
        // The event will be picked up by ApplicationDbContext.SaveChangesAsync and put into OutboxMessages

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Domain event is now handled by the Outbox pattern via ApplicationDbContext

        return Result<Guid>.Success(group.Id);
    }
}