using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Enums; // For FriendshipStatus
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using IMSystem.Server.Domain.Events.Friends;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Friends.Commands;

public class BlockFriendCommandHandler : IRequestHandler<BlockFriendCommand, Result>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BlockFriendCommandHandler> _logger;
    private readonly IMediator _mediator;

    public BlockFriendCommandHandler(
        IFriendshipRepository friendshipRepository,
        IUnitOfWork unitOfWork,
        ILogger<BlockFriendCommandHandler> logger,
        IMediator mediator)
    {
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<Result> Handle(BlockFriendCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {CurrentUserId} attempting to block user {FriendToBlockUserId}", request.CurrentUserId, request.FriendToBlockUserId);

        var friendship = await _friendshipRepository.GetFriendshipBetweenUsersAsync(request.CurrentUserId, request.FriendToBlockUserId);
        bool isNewFriendship = false;

        if (friendship == null)
        {
            _logger.LogInformation("No existing friendship found between User {CurrentUserId} and User {FriendToBlockUserId}. Creating a new blocked relationship.", request.CurrentUserId, request.FriendToBlockUserId);
            // 创建新的 Friendship 记录并直接设置为 Blocked
            friendship = new Domain.Entities.Friendship(request.CurrentUserId, request.FriendToBlockUserId);
            // The Block method will set Status to Blocked, LastModifiedBy, BlockedById, and add DomainEvent
            friendship.Block(request.CurrentUserId);
            await _friendshipRepository.AddAsync(friendship);
            isNewFriendship = true;
        }
        else
        {
            // 如果记录已存在且已被当前用户阻止，则直接返回成功
            if (friendship.Status == FriendshipStatus.Blocked && friendship.BlockedById == request.CurrentUserId)
            {
                _logger.LogInformation("User {FriendToBlockUserId} is already blocked by User {CurrentUserId}. FriendshipId: {FriendshipId}", request.FriendToBlockUserId, request.CurrentUserId, friendship.Id);
                return Result.Success(); // Already blocked by this user.
            }
            // 否则，调用 Block 方法
            friendship.Block(request.CurrentUserId);
            // _friendshipRepository.Update(friendship); // EF Core tracks changes for existing entities
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            if (isNewFriendship)
            {
                _logger.LogInformation("User {CurrentUserId} successfully created a new blocked relationship with user {FriendToBlockUserId}. FriendshipId: {FriendshipId}", request.CurrentUserId, request.FriendToBlockUserId, friendship.Id);
            }
            else
            {
                _logger.LogInformation("User {CurrentUserId} successfully blocked user {FriendToBlockUserId}. FriendshipId: {FriendshipId}", request.CurrentUserId, request.FriendToBlockUserId, friendship.Id);
            }
            
            // 领域事件已在 Friendship.Block() 方法中添加，并将由 DbContext 通过 Outbox 模式处理。
            // 无需在此处显式发布。

            return Result.Success();
        }
        catch (InvalidOperationException ex) // Catch specific exceptions from domain entity if thrown
        {
            _logger.LogWarning(ex, "Invalid operation while User {CurrentUserId} attempting to block user {FriendToBlockUserId}.", request.CurrentUserId, request.FriendToBlockUserId);
            return Result.Failure("Friendship.Block.InvalidOperation", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while User {CurrentUserId} attempting to block user {FriendToBlockUserId}.", request.CurrentUserId, request.FriendToBlockUserId);
            return Result.Failure("Friendship.Block.UnexpectedError", "An error occurred while blocking the friend.");
        }
    }
}