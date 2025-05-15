using IMSystem.Protocol.Common; // For Result
using IMSystem.Server.Domain.Events.FriendGroups;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Exceptions; // For EntityNotFoundException
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

public class DeleteFriendGroupCommandHandler : IRequestHandler<DeleteFriendGroupCommand, Result>
{
    private readonly IFriendGroupRepository _friendGroupRepository;
    private readonly IUserFriendGroupRepository _userFriendGroupRepository; // Added
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteFriendGroupCommandHandler> _logger;

    public DeleteFriendGroupCommandHandler(
        IFriendGroupRepository friendGroupRepository,
        IUserFriendGroupRepository userFriendGroupRepository, // Added
        IUnitOfWork unitOfWork,
        ILogger<DeleteFriendGroupCommandHandler> logger)
    {
        _friendGroupRepository = friendGroupRepository;
        _userFriendGroupRepository = userFriendGroupRepository; // Added
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteFriendGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("用户 {CurrentUserId} 尝试删除好友分组 {GroupId}。", request.CurrentUserId, request.GroupId);

        var groupToDelete = await _friendGroupRepository.GetByIdAsync(request.GroupId);

        if (groupToDelete == null)
        {
            _logger.LogWarning("删除好友分组失败：未找到ID为 {GroupId} 的分组。", request.GroupId);
            return Result.Failure("FriendGroup.NotFound", $"未找到ID为 {request.GroupId} 的分组。");
        }

        // 验证操作用户是否是分组的拥有者
        if (groupToDelete.CreatedBy != request.CurrentUserId)
        {
            _logger.LogWarning("删除好友分组失败：用户 {CurrentUserId} 无权删除不属于自己的分组 {GroupId} (拥有者: {OwnerId})。",
                request.CurrentUserId, request.GroupId, groupToDelete.CreatedBy);
            return Result.Failure("FriendGroup.AccessDenied", "您无权删除此好友分组。");
        }

        // 检查是否为默认分组
        if (groupToDelete.IsDefault)
        {
            _logger.LogWarning("用户 {CurrentUserId} 尝试删除默认分组 {GroupId} (名称: '{GroupName}')，操作被拒绝。",
                request.CurrentUserId, request.GroupId, groupToDelete.Name);
            return Result.Failure("FriendGroup.CannotDeleteDefault", "默认分组不能被删除。");
        }

        // 获取用户的默认分组
        var defaultGroup = await _friendGroupRepository.GetDefaultByUserIdAsync(request.CurrentUserId);
        if (defaultGroup == null)
        {
            // 理论上每个用户都应该有一个默认分组。如果不存在，这是一个严重问题。
            _logger.LogError("严重错误：用户 {CurrentUserId} 没有默认好友分组。无法移动被删除分组 {GroupId} 中的好友。",
                request.CurrentUserId, request.GroupId);
            // 根据业务决定是抛出异常阻止删除，还是允许删除（此时好友将无家可归或被级联删除）
            // 当前选择抛出异常，因为移动好友是核心需求。
            return Result.Failure("FriendGroup.DefaultGroupMissing", $"未能找到用户 {request.CurrentUserId} 的默认好友分组，无法继续删除操作。");
        }
        
        // 如果要删除的分组恰好是默认分组（理论上已被上面的 IsDefault 检查拦截，但作为双重保险）
        if (defaultGroup.Id == groupToDelete.Id)
        {
             _logger.LogWarning("用户 {CurrentUserId} 尝试删除默认分组 {GroupId} (ID与获取到的默认分组ID一致)，操作被拒绝。",
                request.CurrentUserId, request.GroupId);
            return Result.Failure("FriendGroup.CannotDeleteDefault", "默认分组不能被删除。");
        }


        // 获取要删除分组中的所有好友关联
        var friendsInGroupToDelete = await _userFriendGroupRepository.GetByFriendGroupIdAsync(request.GroupId);
        var friendsMovedCount = 0;

        if (friendsInGroupToDelete.Any())
        {
            _logger.LogInformation("分组 {GroupId} (名称: '{GroupName}') 包含 {Count} 个好友，将尝试移动到默认分组 {DefaultGroupId} (名称: '{DefaultGroupName}')。",
                request.GroupId, groupToDelete.Name, friendsInGroupToDelete.Count(), defaultGroup.Id, defaultGroup.Name);

            foreach (var ufgInDeletedGroup in friendsInGroupToDelete)
            {
                // 1. 从旧分组中移除 (EF Core 会跟踪此移除)
                _userFriendGroupRepository.Remove(ufgInDeletedGroup);

                // 2. 检查该好友是否已在默认分组中
                var existingInDefaultGroup = await _userFriendGroupRepository.GetByFriendGroupIdAndFriendshipIdAsync(defaultGroup.Id, ufgInDeletedGroup.FriendshipId);
                if (existingInDefaultGroup == null)
                {
                    // 3. 如果不在，则创建新的关联到默认分组
                    var newUfgForDefaultGroup = new UserFriendGroup(
                        request.CurrentUserId, // 分组操作者，也是默认分组的拥有者
                        ufgInDeletedGroup.FriendshipId,
                        defaultGroup.Id
                    );
                    await _userFriendGroupRepository.AddAsync(newUfgForDefaultGroup);
                    friendsMovedCount++;
                }
                else
                {
                    _logger.LogInformation("好友 (FriendshipId: {FriendshipId}) 已存在于默认分组 {DefaultGroupId}，无需重复添加。",
                        ufgInDeletedGroup.FriendshipId, defaultGroup.Id);
                }
            }
            // SaveChangesAsync 将在所有操作准备好后统一调用
            if (friendsMovedCount > 0)
            {
                _logger.LogInformation("已准备将 {FriendsMovedCount} 个好友从分组 {GroupId} 移动到默认分组 {DefaultGroupId}。",
                    friendsMovedCount, request.GroupId, defaultGroup.Id);
            }
        }
        else
        {
            _logger.LogInformation("分组 {GroupId} (名称: '{GroupName}') 为空，无需移动好友。", request.GroupId, groupToDelete.Name);
        }

        // 现在可以安全删除原分组 (此时它应该是空的，或者其下的 UserFriendGroup 记录已被标记为删除)
        _friendGroupRepository.Remove(groupToDelete);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("用户 {CurrentUserId} 成功删除了好友分组 {GroupId} (名称: '{GroupName}')。{FriendsMovedCount} 个好友（如有）已被移动到默认分组。",
            request.CurrentUserId, request.GroupId, groupToDelete.Name, friendsMovedCount);

            // 触发领域事件 FriendGroupDeletedEvent
            groupToDelete.AddDomainEvent(new FriendGroupDeletedEvent(groupToDelete.Id, groupToDelete.Name, request.CurrentUserId));

        return Result.Success();
    }
}