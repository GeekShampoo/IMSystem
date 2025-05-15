using AutoMapper;
using IMSystem.Protocol.DTOs.Requests.Friends;
using IMSystem.Protocol.DTOs.Responses.Friends;
using IMSystem.Server.Core.Features.Friends.Commands;
using IMSystem.Server.Core.Features.Friends.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using IMSystem.Protocol.Common;
using IMSystem.Server.Domain.Exceptions;
using IMSystem.Server.Web.Common;

namespace IMSystem.Server.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 所有好友相关的操作都需要认证
public class FriendsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<FriendsController> _logger;

    public FriendsController(IMediator mediator, IMapper mapper, ILogger<FriendsController> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 发送好友请求给指定用户。
    /// </summary>
    /// <param name="request">包含目标用户ID的请求。</param>
    /// <returns>成功则返回201 Created及好友请求ID；失败则返回错误信息。</returns>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)] // 返回 FriendshipId
    // 404和其他错误码由Post约定和HandleResult覆盖
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestRequest request)
    {
        try
        {
            var command = new SendFriendRequestCommand(CurrentUserId, request.AddresseeId, request.RequesterRemark);
            var result = await _mediator.Send(command);

            _logger.LogInformation("用户 {RequesterId} 向用户 {AddresseeId} 发送好友请求", CurrentUserId, request.AddresseeId);
            
            return HandleResult(result, 
                friendshipId => CreatedAtAction(nameof(GetFriendshipById), new { friendshipId }, new { friendshipId }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送好友请求过程中发生意外错误 (Requester: {RequesterId}, Addressee: {AddresseeId})。", CurrentUserId, request.AddresseeId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, "发送好友请求过程中发生内部错误，请稍后重试", 
                errorCode: "FriendRequest.Send.UnexpectedError"));
        }
    }
    
    /// <summary>
    /// 根据ID获取指定的好友关系信息。
    /// </summary>
    /// <param name="friendshipId">好友关系ID。</param>
    /// <returns>好友关系信息 (通常是对方用户的信息及关系状态)；如果未找到或无权访问，则返回404或403。</returns>
    [HttpGet("requests/{friendshipId:guid}", Name = "GetFriendshipById")]
    [ProducesResponseType(typeof(FriendDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)] // 保留，因为直接返回NotFound
    // 其他错误码由GetById约定覆盖
    public async Task<IActionResult> GetFriendshipById(Guid friendshipId)
    {
        if (friendshipId == Guid.Empty)
        {
            _logger.LogWarning("GetFriendshipById 请求中的好友关系ID为空。");
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "好友关系ID不能为空", errorCode: "Validation.GuidEmpty"));
        }

        _logger.LogInformation("用户 {UserId} 请求获取好友关系ID为 {FriendshipId} 的信息。", CurrentUserId, friendshipId);
        try
        {
            var query = new GetFriendshipByIdQuery(friendshipId, CurrentUserId);
            var friendDto = await _mediator.Send(query);

            if (friendDto == null)
            {
                _logger.LogWarning("未能找到好友关系ID为 {FriendshipId}，或用户 {UserId} 无权访问。", friendshipId, CurrentUserId);
                return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, "未能找到好友关系ID，或您无权访问", 
                    errorCode: "Friendship.NotFoundOrNoAccess"));
            }

            _logger.LogInformation("成功获取好友关系ID为 {FriendshipId} 的信息 (用户: {UserId})。", friendshipId, CurrentUserId);
            return Ok(friendDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("获取好友关系信息参数错误 (FriendshipId: {FriendshipId}, UserId: {UserId}): {ErrorMessage}", friendshipId, CurrentUserId, ex.Message);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Validation.ArgumentError"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取好友关系ID {FriendshipId} 的信息过程中发生意外错误 (用户: {UserId})。", friendshipId, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, "获取好友关系信息过程中发生内部错误，请稍后重试", 
                errorCode: "Friendship.GetById.UnexpectedError"));
        }
    }

    /// <summary>
    /// 获取当前用户收到的待处理好友请求列表。
    /// </summary>
    /// <returns>待处理好友请求列表。</returns>
    [HttpGet("requests/pending")]
    [ProducesResponseType(typeof(IEnumerable<FriendRequestDto>), StatusCodes.Status200OK)]
    // 其他错误码由GetList约定和HandleResult覆盖
    public async Task<IActionResult> GetPendingFriendRequests()
    {
        try
        {
            var query = new GetPendingFriendRequestsQuery(CurrentUserId);
            var result = await _mediator.Send(query);

            return HandleResult(result, value => Ok(value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户 {UserId} 的待处理好友请求列表过程中发生意外错误。", CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, "获取待处理好友请求列表过程中发生内部错误，请稍后重试", 
                errorCode: "FriendRequest.GetPending.UnexpectedError"));
        }
    }

    /// <summary>
    /// 接受一个好友请求。
    /// </summary>
    /// <param name="requestId">要接受的好友请求ID (FriendshipId)。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPut("requests/{requestId}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // 其他错误码由DoAction约定和HandleResult覆盖
    public async Task<IActionResult> AcceptFriendRequest(Guid requestId)
    {
        try
        {
            var command = new AcceptFriendRequestCommand(requestId, CurrentUserId);
            var result = await _mediator.Send(command);

            return HandleResult(result, () => NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "接受好友请求 {RequestId} 过程中发生意外错误 (操作用户: {UserId})。", requestId, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, "接受好友请求过程中发生内部错误，请稍后重试", 
                errorCode: "FriendRequest.Accept.UnexpectedError"));
        }
    }

    /// <summary>
    /// 拒绝一个好友请求。
    /// </summary>
    /// <param name="requestId">要拒绝的好友请求ID (FriendshipId)。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPut("requests/{requestId}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // 其他错误码由DoAction约定和HandleResult覆盖
    public async Task<IActionResult> DeclineFriendRequest(Guid requestId)
    {
        try
        {
            var command = new DeclineFriendRequestCommand(requestId, CurrentUserId);
            var result = await _mediator.Send(command);

            return HandleResult(result, () => NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "拒绝好友请求 {RequestId} 过程中发生意外错误 (操作用户: {UserId})。", requestId, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, "拒绝好友请求过程中发生内部错误，请稍后重试", 
                errorCode: "FriendRequest.Decline.UnexpectedError"));
        }
    }

    /// <summary>
    /// 获取当前用户的好友列表。
    /// </summary>
    /// <returns>当前用户的好友列表。</returns>
    [HttpGet] // Route will be "api/friends"
    [ProducesResponseType(typeof(PagedResult<FriendDto>), StatusCodes.Status200OK)]
    // 其他错误码由GetPaged约定和HandleResult覆盖
    public async Task<IActionResult> GetFriends([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("用户 {UserId} 请求获取好友列表。", CurrentUserId);
        try
        {
            var query = new GetFriendsQuery(CurrentUserId, pageNumber, pageSize);
            var result = await _mediator.Send(query);

            return HandleResult(result, value => Ok(value));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("获取好友列表参数错误 (UserId: {UserId}): {ErrorMessage}", CurrentUserId, ex.Message);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Validation.ArgumentError"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为用户 {UserId} 获取好友列表过程中发生意外错误。", CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, "获取好友列表过程中发生内部错误，请稍后重试", 
                errorCode: "Friend.GetList.UnexpectedError"));
        }
    }

    /// <summary>
    /// 移除一个好友。
    /// </summary>
    /// <param name="friendUserId">要移除的好友的用户ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpDelete("{friendUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // 其他错误码由Delete约定和HandleResult覆盖
    public async Task<IActionResult> RemoveFriend(Guid friendUserId)
    {
        if (friendUserId == Guid.Empty)
        {
            _logger.LogWarning("移除好友请求中的 friendUserId 为空。");
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "好友用户ID不能为空", errorCode: "Validation.GuidEmpty"));
        }

        if (CurrentUserId == friendUserId)
        {
            _logger.LogWarning("用户 {UserId} 尝试移除自己为好友。", CurrentUserId);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "不能将自己移除出好友列表", errorCode: "Friend.Remove.CannotRemoveSelf"));
        }

        _logger.LogInformation("用户 {CurrentUserId} 请求移除好友 {FriendUserId}。", CurrentUserId, friendUserId);

        try
        {
            var command = new RemoveFriendCommand(CurrentUserId, friendUserId);
            var result = await _mediator.Send(command);

            return HandleResult(result, () => NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户 {CurrentUserId} 移除好友 {FriendUserId} 过程中发生意外错误。", CurrentUserId, friendUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, "移除好友过程中发生内部错误，请稍后重试", 
                errorCode: "Friend.Remove.UnexpectedError"));
        }
    }

    /// <summary>
    /// 屏蔽好友。
    /// </summary>
    /// <param name="friendUserId">要屏蔽的好友的用户 ID。</param>
    /// <returns>如果成功则无内容，否则返回错误响应。</returns>
    [HttpPost("{friendUserId:guid}/block")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // 其他错误码由DoAction约定和HandleResult覆盖
    public async Task<IActionResult> BlockFriend(Guid friendUserId)
    {
        if (friendUserId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "Friend user ID cannot be empty.", errorCode: "Validation.GuidEmpty"));
        }

        if (CurrentUserId == friendUserId)
        {
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "Cannot block oneself.", errorCode: "Friend.Block.CannotBlockSelf"));
        }

        _logger.LogInformation("User {CurrentUserId} attempting to block friend {FriendUserId}.", CurrentUserId, friendUserId);
        var command = new BlockFriendCommand(CurrentUserId, friendUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 取消屏蔽好友。
    /// </summary>
    /// <param name="friendUserId">要取消屏蔽的好友的用户 ID。</param>
    /// <returns>如果成功则无内容，否则返回错误响应。</returns>
    [HttpPost("{friendUserId:guid}/unblock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // 其他错误码由DoAction约定和HandleResult覆盖
    public async Task<IActionResult> UnblockFriend(Guid friendUserId)
    {
        if (friendUserId == Guid.Empty)
        {
            var errorResponse = new ApiErrorResponse(StatusCodes.Status400BadRequest, "好友用户ID不能为空", errorCode: "Validation.GuidEmpty");
            return BadRequest(errorResponse);
        }

        _logger.LogInformation("User {CurrentUserId} attempting to unblock friend {FriendUserId}.", CurrentUserId, friendUserId);
        var command = new UnblockFriendCommand(CurrentUserId, friendUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// Sets or updates the remark for a friend.
    /// </summary>
    /// <param name="friendUserId">The user ID of the friend for whom to set the remark.</param>
    /// <param name="request">The request containing the new remark.</param>
    /// <returns>204 NoContent if successful, otherwise an error response.</returns>
    [HttpPut("{friendUserId:guid}/remark")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)] // 保留，因为直接处理EntityNotFoundException
    // 其他错误码由Put约定和HandleResult覆盖
    public async Task<IActionResult> SetFriendRemark(Guid friendUserId, [FromBody] SetFriendRemarkRequest request)
    {
        if (friendUserId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "Friend user ID cannot be empty.", errorCode: "Validation.GuidEmpty"));
        }

        if (request == null)
        {
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "Request body cannot be null.", errorCode: "Validation.RequestBodyNull"));
        }
        
        if (CurrentUserId == friendUserId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to set a remark for themselves.", CurrentUserId);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "Cannot set a remark for oneself.", errorCode: "Friend.Remark.CannotSetForSelf"));
        }

        _logger.LogInformation("User {CurrentUserId} attempting to set remark for friend {FriendUserId}.", CurrentUserId, friendUserId);

        try
        {
            var command = new SetFriendRemarkCommand
            {
                CurrentUserId = CurrentUserId,
                FriendUserId = friendUserId,
                Remark = request.Remark
            };

            var result = await _mediator.Send(command);
            
            return HandleResult(result, () => NoContent());
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Set remark failed: Friendship not found between user {CurrentUserId} and {FriendUserId}.", CurrentUserId, friendUserId);
            return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, ex.Message, errorCode: "Friendship.NotFound"));
        }
        catch (InvalidOperationException ex)
        {
             _logger.LogWarning(ex, "Set remark failed due to invalid operation for user {CurrentUserId} and friend {FriendUserId}.", CurrentUserId, friendUserId);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Friend.Remark.InvalidOperation"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while setting remark for friend {FriendUserId} by user {CurrentUserId}.", friendUserId, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, "An unexpected error occurred while setting the friend remark.", 
                errorCode: "Friend.Remark.UnexpectedError"));
        }
    }
}