using IMSystem.Protocol.Common; // For Result
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Events.FriendGroups; // For FriendGroupUpdatedEvent
using IMSystem.Server.Domain.Exceptions; // For EntityNotFoundException
using MediatR;
using IMSystem.Server.Core.Constants; // For FriendGroupConstants
// using Microsoft.EntityFrameworkCore; // Removed EF Core dependency
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

public class UpdateFriendGroupCommandHandler : IRequestHandler<UpdateFriendGroupCommand, Result>
{
    private readonly IFriendGroupRepository _friendGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateFriendGroupCommandHandler> _logger;
    private readonly IPublisher _publisher; // Added for publishing domain events

    public UpdateFriendGroupCommandHandler(
        IFriendGroupRepository friendGroupRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateFriendGroupCommandHandler> logger,
        IPublisher publisher) // Added IPublisher
    {
        _friendGroupRepository = friendGroupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _publisher = publisher; // Added IPublisher
    }

    public async Task<Result> Handle(UpdateFriendGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("用户 {CurrentUserId} 尝试更新好友分组 {GroupId}。新名称: '{NewName}', 新排序: {NewOrder}",
            request.CurrentUserId, request.GroupId, request.NewName ?? "未更改", request.NewOrder?.ToString() ?? "未更改");

        var friendGroup = await _friendGroupRepository.GetByIdAsync(request.GroupId);

        if (friendGroup == null)
        {
            _logger.LogWarning("更新好友分组失败：未找到ID为 {GroupId} 的分组。", request.GroupId);
            return Result.Failure("FriendGroup.NotFound", $"未找到ID为 {request.GroupId} 的分组。");
        }

        // 验证操作用户是否是分组的拥有者
        if (friendGroup.CreatedBy != request.CurrentUserId)
        {
            _logger.LogWarning("更新好友分组失败：用户 {CurrentUserId} 无权修改不属于自己的分组 {GroupId} (拥有者: {OwnerId})。",
                request.CurrentUserId, request.GroupId, friendGroup.CreatedBy);
            return Result.Failure("FriendGroup.AccessDenied", "您无权修改此好友分组。");
        }

        // 检查是否为默认分组，如果是，则限制修改
        if (friendGroup.IsDefault)
        {
            // 检查是否尝试修改默认分组的名称
            if (!string.IsNullOrWhiteSpace(request.NewName) && !request.NewName.Equals(FriendGroupConstants.DefaultGroupName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("用户 {CurrentUserId} 尝试修改默认分组 {GroupId} 的名称为 '{NewName}'，操作被拒绝。",
                    request.CurrentUserId, request.GroupId, request.NewName);
                return Result.Failure("FriendGroup.CannotModifyDefaultName", $"默认分组 '{FriendGroupConstants.DefaultGroupName}' 的名称不能被修改。");
            }
            // 检查是否尝试修改默认分组的排序
            if (request.NewOrder.HasValue && request.NewOrder.Value != FriendGroupConstants.DefaultGroupOrder)
            {
                _logger.LogWarning("用户 {CurrentUserId} 尝试修改默认分组 {GroupId} 的排序为 {NewOrder}，操作被拒绝。",
                    request.CurrentUserId, request.GroupId, request.NewOrder.Value);
                return Result.Failure("FriendGroup.CannotModifyDefaultOrder", $"默认分组 '{FriendGroupConstants.DefaultGroupName}' 的排序不能被修改。");
            }
            // 如果没有提供新名称或新排序，或者提供的值与默认值相同，则不执行任何操作
            if (string.IsNullOrWhiteSpace(request.NewName) && !request.NewOrder.HasValue)
            {
                 _logger.LogInformation("默认分组 {GroupId} 的信息未发生变化，无需更新。", request.GroupId);
                return Result.Success();
            }
            // 如果提供的名称/排序与默认值一致，也视为无变化
            bool nameIsSameAsDefault = string.IsNullOrWhiteSpace(request.NewName) || request.NewName.Equals(FriendGroupConstants.DefaultGroupName, StringComparison.OrdinalIgnoreCase);
            bool orderIsSameAsDefault = !request.NewOrder.HasValue || request.NewOrder.Value == FriendGroupConstants.DefaultGroupOrder;

            if (nameIsSameAsDefault && orderIsSameAsDefault)
            {
                _logger.LogInformation("默认分组 {GroupId} 的信息未发生变化（与默认值一致），无需更新。", request.GroupId);
                return Result.Success();
            }
        }
        else // 非默认分组
        {
            // 如果提供了新名称，检查新名称是否与该用户的其他分组冲突 (排除当前分组自身)
            if (!string.IsNullOrWhiteSpace(request.NewName) && !request.NewName.Equals(friendGroup.Name, StringComparison.OrdinalIgnoreCase))
            {
                // 检查新名称是否与预定义的默认分组名称冲突
                if (string.Equals(request.NewName, FriendGroupConstants.DefaultGroupName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("更新好友分组失败：用户 {CurrentUserId} 尝试将分组 {GroupId} 的名称更改为保留的默认分组名称 '{DefaultGroupName}'。",
                        request.CurrentUserId, request.GroupId, FriendGroupConstants.DefaultGroupName);
                    return Result.Failure("FriendGroup.ReservedName", $"分组名称 '{request.NewName}' 是保留名称，不允许使用。");
                }

                var existingGroupWithNewName = await _friendGroupRepository.GetByNameAndUserIdAsync(request.NewName, request.CurrentUserId);
                if (existingGroupWithNewName != null && existingGroupWithNewName.Id != request.GroupId)
                {
                    _logger.LogWarning("更新好友分组失败：用户 {CurrentUserId} 已存在名为 '{NewName}' 的分组。", request.CurrentUserId, request.NewName);
                    return Result.Failure("FriendGroup.NameConflict", $"您已拥有一个名为 '{request.NewName}' 的分组。请使用其他名称。");
                }
            }
        }

        // 如果提供了新排序值，并且与当前排序值不同，则检查其唯一性
        if (request.NewOrder.HasValue && request.NewOrder.Value != friendGroup.Order)
        {
            // 检查新的 Order 是否已被该用户的其他分组占用
            // (理论上，如果 ExistsByUserIdAndOrderAsync 查找时不排除当前 group ID，可能会误判)
            // 但由于我们是先获取 friendGroup，再检查新 Order，所以如果新 Order 与现有某个其他分组的 Order 相同，则会检测到。
            // 如果新 Order 与当前分组的旧 Order 相同，则上面的条件 request.NewOrder.Value != friendGroup.Order 会阻止此检查。
            // var existingGroupWithNewOrder = await _friendGroupRepository.Queryable() // Assuming Queryable is on IFriendGroupRepository or its base
            //     .FirstOrDefaultAsync(fg => fg.CreatedBy == request.CurrentUserId && fg.Order == request.NewOrder.Value && fg.Id != request.GroupId, cancellationToken);
            var existingGroupWithNewOrder = await _friendGroupRepository.GetByUserIdAndOrderExcludingGroupIdAsync(request.CurrentUserId, request.NewOrder.Value, request.GroupId);


            if (existingGroupWithNewOrder != null)
            {
                _logger.LogWarning("更新好友分组失败：用户 {CurrentUserId} 已存在 Order 为 {NewOrder} 的分组 (ID: {ExistingGroupId})。",
                    request.CurrentUserId, request.NewOrder.Value, existingGroupWithNewOrder.Id);
                return Result.Failure("FriendGroup.OrderConflict", $"您已拥有一个排序值为 {request.NewOrder.Value} 的分组。请选择其他排序值。");
            }
        }


        // 更新实体
        string nameToUpdate = string.IsNullOrWhiteSpace(request.NewName) ? friendGroup.Name : request.NewName;
        int orderToUpdate = request.NewOrder.HasValue ? request.NewOrder.Value : friendGroup.Order;
        
        string oldName = friendGroup.Name;
        int oldOrder = friendGroup.Order;
        bool changed = false;

        // 只有当名称或排序实际发生变化时才调用 UpdateDetails
        if (!nameToUpdate.Equals(oldName, StringComparison.Ordinal) || orderToUpdate != oldOrder)
        {
            friendGroup.UpdateDetails(nameToUpdate, orderToUpdate, request.CurrentUserId);
            // _friendGroupRepository.Update(friendGroup); // EF Core tracks changes, explicit Update not always needed.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("用户 {CurrentUserId} 成功更新了好友分组 {GroupId}。新名称: '{NewName}', 新排序: {NewOrder}",
                request.CurrentUserId, request.GroupId, nameToUpdate, orderToUpdate);
            changed = true;
        }
        else
        {
            _logger.LogInformation("好友分组 {GroupId} 的信息未发生变化，无需更新。", request.GroupId);
        }
        
        if (changed)
        {
            var updatedEvent = new FriendGroupUpdatedEvent(
                friendGroup.Id,
                friendGroup.CreatedBy!.Value, // UserId
                oldName,
                friendGroup.Name, // New name
                oldOrder,
                friendGroup.Order, // New order
                friendGroup.IsDefault
            );
            // 领域事件统一通过实体 AddDomainEvent 添加，禁止直接 Publish，事件将由 Outbox 机制可靠交付
            friendGroup.AddDomainEvent(updatedEvent);
        }

        return Result.Success();
    }
}