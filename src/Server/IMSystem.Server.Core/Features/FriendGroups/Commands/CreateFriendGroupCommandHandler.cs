using AutoMapper;
using IMSystem.Protocol.Common; // For Result
using IMSystem.Protocol.DTOs.Responses.FriendGroups;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Events.FriendGroups; // For FriendGroupCreatedEvent
using MediatR;
using Microsoft.Extensions.Logging;
using IMSystem.Server.Core.Constants; // For FriendGroupConstants
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

public class CreateFriendGroupCommandHandler : IRequestHandler<CreateFriendGroupCommand, Result<FriendGroupDto>>
{
    private readonly IFriendGroupRepository _friendGroupRepository;
    private readonly IUserRepository _userRepository; // To verify user existence
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateFriendGroupCommandHandler> _logger;
    private readonly IPublisher _publisher; // Added for publishing domain events

    public CreateFriendGroupCommandHandler(
        IFriendGroupRepository friendGroupRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateFriendGroupCommandHandler> logger,
        IPublisher publisher) // Added IPublisher
    {
        _friendGroupRepository = friendGroupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _publisher = publisher; // Added IPublisher
    }

    public async Task<Result<FriendGroupDto>> Handle(CreateFriendGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("用户 {UserId} 尝试创建好友分组，名称为 '{GroupName}'。", request.UserId, request.Name);

        // 1. 验证用户是否存在 (虽然 UserId 来自认证上下文，但以防万一)
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning("创建好友分组失败：用户 {UserId} 不存在。", request.UserId);
            return Result<FriendGroupDto>.Failure("User.NotFound", $"用户 (ID: {request.UserId}) 不存在。");
        }

        // 2. 检查用户是否已存在同名分组
        var existingGroup = await _friendGroupRepository.GetByNameAndUserIdAsync(request.Name, request.UserId);
        if (existingGroup != null)
        {
            _logger.LogWarning("创建好友分组失败：用户 {UserId} 已存在名为 '{GroupName}' 的分组。", request.UserId, request.Name);
            return Result<FriendGroupDto>.Failure("FriendGroup.NameConflict", $"您已拥有一个名为 '{request.Name}' 的分组。");
        }

        // 3. 检查是否尝试创建与默认分组同名的分组
        if (string.Equals(request.Name, FriendGroupConstants.DefaultGroupName, StringComparison.OrdinalIgnoreCase))
        {
            // 检查用户是否已经有了默认分组，理论上应该有，但以防万一
            var defaultGroup = await _friendGroupRepository.GetDefaultByUserIdAsync(request.UserId);
            if (defaultGroup != null && string.Equals(defaultGroup.Name, FriendGroupConstants.DefaultGroupName, StringComparison.OrdinalIgnoreCase))
            {
                 _logger.LogWarning("创建好友分组失败：用户 {UserId} 尝试创建与现有默认分组 '{DefaultGroupName}' 同名的分组。",
                    request.UserId, FriendGroupConstants.DefaultGroupName);
                return Result<FriendGroupDto>.Failure("FriendGroup.ReservedName", $"分组名称 '{request.Name}' 是保留名称，不允许创建。");
            }
            // 如果用户没有默认分组（异常情况），或者默认分组名称已被修改（如果允许），
            // 这里的逻辑可能需要调整。当前假设默认分组名称固定且用户已拥有。
            // 更简单的做法是，只要名称匹配预定义的默认名称，就禁止创建。
            _logger.LogWarning("创建好友分组失败：用户 {UserId} 尝试使用保留的默认分组名称 '{DefaultGroupName}'。",
                request.UserId, FriendGroupConstants.DefaultGroupName);
            return Result<FriendGroupDto>.Failure("FriendGroup.ReservedName", $"分组名称 '{request.Name}' 是保留名称，不允许使用。");
        }

        // 4. 检查 Order 是否已存在
        if (await _friendGroupRepository.ExistsByUserIdAndOrderAsync(request.UserId, request.Order))
        {
            _logger.LogWarning("创建好友分组失败：用户 {UserId} 已存在 Order 为 {Order} 的分组。", request.UserId, request.Order);
            return Result<FriendGroupDto>.Failure("FriendGroup.OrderConflict", $"您已拥有一个排序值为 {request.Order} 的分组。请选择其他排序值。");
        }

        // 5. 创建并保存好友分组
        // 用户创建的分组 isDefault 始终为 false (由 FriendGroup 构造函数默认处理)
        var newFriendGroup = new FriendGroup(request.UserId, request.Name, request.Order); // isDefault 默认为 false
        
        await _friendGroupRepository.AddAsync(newFriendGroup);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("用户 {UserId} 成功创建好友分组 '{GroupName}' (ID: {GroupId})。", request.UserId, newFriendGroup.Name, newFriendGroup.Id);
        
        // 将新创建的 FriendGroup 实体映射到 FriendGroupDto
        var friendGroupDto = _mapper.Map<FriendGroupDto>(newFriendGroup);
        
        // Publish domain event
        var createdEvent = new FriendGroupCreatedEvent(
            newFriendGroup.Id,
            newFriendGroup.CreatedBy!.Value, // CreatedBy is UserId, should not be null
            newFriendGroup.Name,
            newFriendGroup.Order,
            newFriendGroup.IsDefault
        );
        // 领域事件统一通过实体 AddDomainEvent 添加，禁止直接 Publish，事件将由 Outbox 机制可靠交付
        newFriendGroup.AddDomainEvent(createdEvent);

        return Result<FriendGroupDto>.Success(friendGroupDto);
    }
}