using AutoMapper;
using IMSystem.Protocol.DTOs.Requests.FriendGroups;
using IMSystem.Protocol.DTOs.Responses.FriendGroups;
using IMSystem.Server.Core.Features.FriendGroups.Commands;
using IMSystem.Server.Core.Features.FriendGroups.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Exceptions;
using System.Collections.Generic;
using IMSystem.Protocol.Common;
using IMSystem.Server.Web.Common;

namespace IMSystem.Server.Web.Controllers;

[ApiController]
[Route("api/friend-groups")]
[Authorize] // 所有好友分组操作都需要认证
public class FriendGroupsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<FriendGroupsController> _logger;

    public FriendGroupsController(IMediator mediator, IMapper mapper, ILogger<FriendGroupsController> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 创建一个新的好友分组。
    /// </summary>
    /// <param name="request">包含分组名称和排序信息的请求。</param>
    /// <returns>成功则返回201 Created及创建的分组信息；失败则返回错误信息。</returns>
    [HttpPost]
    [ProducesResponseType(typeof(FriendGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)] // 例如分组名已存在
    // 其他错误码由Post约定和HandleResult覆盖
    public async Task<IActionResult> CreateFriendGroup([FromBody] CreateFriendGroupRequest request)
    {
        try
        {
            var command = new CreateFriendGroupCommand(CurrentUserId, request.Name, request.Order);
            var result = await _mediator.Send(command);

            _logger.LogInformation("用户 {UserId} 请求创建好友分组 '{GroupName}'", CurrentUserId, request.Name);

            return HandleResult(result, 
                friendGroupDto => CreatedAtAction(
                    nameof(GetFriendGroupById), 
                    new { groupId = friendGroupDto.GroupId }, 
                    friendGroupDto),
                conflictErrorCode: "FriendGroup.NameConflict");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建好友分组过程中发生意外错误 (用户: {UserId}, 分组名: {GroupName})。",
                CurrentUserId, request.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, 
                    "创建好友分组过程中发生内部错误，请稍后重试", 
                    errorCode: "FriendGroup.Create.UnexpectedError"));
        }
    }

    /// <summary>
    /// 根据ID获取指定的好友分组信息。
    /// </summary>
    /// <param name="groupId">好友分组ID。</param>
    /// <returns>好友分组信息；如果未找到或无权访问，则返回404或403。</returns>
    [HttpGet("{groupId:guid}", Name = "GetFriendGroupById")]
    [ProducesResponseType(typeof(FriendGroupDto), StatusCodes.Status200OK)]
    // 其他错误码由GetById约定和BaseApiController.HandleResult覆盖
    public async Task<IActionResult> GetFriendGroupById(Guid groupId)
    {
        if (groupId == Guid.Empty)
        {
            _logger.LogWarning("GetFriendGroupById 请求中的分组ID为空。");
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "分组ID不能为空", errorCode: "Validation.GuidEmpty"));
        }

        _logger.LogInformation("用户 {UserId} 请求获取好友分组ID为 {GroupId} 的信息。", CurrentUserId, groupId);
        try
        {
            var query = new GetFriendGroupByIdQuery(groupId, CurrentUserId);
            var friendGroupDto = await _mediator.Send(query);

            if (friendGroupDto == null)
            {
                _logger.LogWarning("未能找到好友分组ID为 {GroupId}，或用户 {UserId} 无权访问。", groupId, CurrentUserId);
                return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, 
                    $"未能找到好友分组ID为 '{groupId}'，或您无权访问。", 
                    errorCode: "FriendGroup.NotFoundOrNoAccess"));
            }

            _logger.LogInformation("成功获取好友分组ID为 {GroupId} 的信息 (用户: {UserId})。", groupId, CurrentUserId);
            return Ok(friendGroupDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("获取好友分组信息参数错误 (GroupId: {GroupId}, UserId: {UserId}): {ErrorMessage}", groupId, CurrentUserId, ex.Message);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Validation.ArgumentError"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取好友分组ID {GroupId} 的信息过程中发生意外错误 (用户: {UserId})。", groupId, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, 
                    "获取好友分组信息过程中发生内部错误，请稍后重试", 
                    errorCode: "FriendGroup.GetById.UnexpectedError"));
        }
    }

    /// <summary>
    /// 获取当前用户的所有好友分组。
    /// </summary>
    /// <returns>好友分组列表。</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FriendGroupDto>), StatusCodes.Status200OK)]
    // 其他错误码由GetList约定和HandleResult覆盖
    public async Task<IActionResult> GetUserFriendGroups()
    {
        try
        {
            var query = new GetUserFriendGroupsQuery(CurrentUserId);
            var result = await _mediator.Send(query);

            return HandleResult(result, value => Ok(value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户 {UserId} 的所有好友分组列表过程中发生意外错误。", CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, 
                    "获取好友分组列表过程中发生内部错误，请稍后重试", 
                    errorCode: "FriendGroup.GetAll.UnexpectedError"));
        }
    }

    /// <summary>
    /// 更新指定好友分组的名称或排序。
    /// </summary>
    /// <param name="groupId">要更新的好友分组ID。</param>
    /// <param name="request">包含新名称和/或新排序的请求体。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPut("{groupId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)] // 名称或排序值冲突
    // 其他错误码由Put约定和HandleResult覆盖
    public async Task<IActionResult> UpdateFriendGroup(Guid groupId, [FromBody] UpdateFriendGroupRequest request)
    {
        try
        {
            var command = new UpdateFriendGroupCommand(groupId, CurrentUserId, request.Name, request.Order);
            var result = await _mediator.Send(command);

            return HandleResult(result, () => NoContent(), conflictErrorCode: "FriendGroup.NameConflict");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新好友分组 {GroupId} 过程中发生意外错误 (操作用户: {UserId})。", groupId, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, 
                    "更新好友分组过程中发生内部错误，请稍后重试", 
                    errorCode: "FriendGroup.Update.UnexpectedError"));
        }
    }

    /// <summary>
    /// 删除指定的好友分组。
    /// </summary>
    /// <param name="groupId">要删除的好友分组ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpDelete("{groupId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // 其他错误码由Delete约定和HandleResult覆盖
    public async Task<IActionResult> DeleteFriendGroup(Guid groupId)
    {
        try
        {
            var command = new DeleteFriendGroupCommand(groupId, CurrentUserId);
            var result = await _mediator.Send(command);

            return HandleResult(result, () => NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除好友分组 {GroupId} 过程中发生意外错误 (操作用户: {UserId})。", groupId, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, 
                    "删除好友分组过程中发生内部错误，请稍后重试", 
                    errorCode: "FriendGroup.Delete.UnexpectedError"));
        }
    }

    /// <summary>
    /// 将指定的好友添加（或移动）到指定的好友分组。
    /// </summary>
    /// <remarks>
    /// 如果好友已在当前用户的其他分组中，此操作会将其从旧分组移动到新分组。
    /// 如果好友已在目标分组中，则不执行任何操作并返回成功。
    /// </remarks>
    /// <param name="groupId">要将好友添加到的分组ID。</param>
    /// <param name="request">包含要添加的好友的 FriendshipId。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPost("{groupId}/friends")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // 其他错误码由DoAction约定和HandleResult覆盖
    public async Task<IActionResult> AddFriendToGroup(Guid groupId, [FromBody] AddFriendToGroupRequest request)
    {
        try
        {
            var command = new AddFriendToGroupCommand(CurrentUserId, groupId, request.FriendshipId);
            var result = await _mediator.Send(command);

            return HandleResult(result, () => NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "将好友 (FriendshipId: {FriendshipId}) 添加到分组 {GroupId} 过程中发生意外错误 (操作用户: {UserId})。",
                request.FriendshipId, groupId, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, 
                    "将好友添加到分组过程中发生内部错误，请稍后重试", 
                    errorCode: "FriendGroup.AddFriend.UnexpectedError"));
        }
    }

    /// <summary>
    /// 对已验证用户的的好友分组进行重新排序。
    /// </summary>
    /// <param name="orderedGroupIds">按所需新顺序排列的好友分组 ID 列表。</param>
    /// <returns>如果成功则无内容，否则返回错误响应。</returns>
    [HttpPost("reorder")] // 对此操作使用 POST
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // 其他错误码由DoAction约定和HandleResult覆盖
    public async Task<IActionResult> ReorderFriendGroups([FromBody] List<Guid> orderedGroupIds)
    {
        if (orderedGroupIds == null || !orderedGroupIds.Any())
        {
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, 
                "Ordered group IDs list cannot be null or empty.", 
                errorCode: "Validation.InputEmpty"));
        }

        _logger.LogInformation("User {UserId} attempting to reorder friend groups.", CurrentUserId);

        try
        {
            var command = new ReorderFriendGroupsCommand(CurrentUserId, orderedGroupIds);
            var result = await _mediator.Send(command);

            return HandleResult(result, () => NoContent());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid arguments for ReorderFriendGroups command for user {UserId}: {ErrorMessage}", CurrentUserId, ex.Message);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Validation.ArgumentError"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering friend groups for user {UserId}.", CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, 
                    "An error occurred while reordering friend groups.", 
                    errorCode: "FriendGroup.Reorder.UnexpectedError"));
        }
    }

    /// <summary>
    /// 将指定的好友移动到当前用户的默认好友分组。
    /// </summary>
    /// <param name="friendshipId">代表要移动的好友的好友关系 ID。</param>
    /// <returns>如果成功则无内容，否则返回错误响应。</returns>
    [HttpPost("friends/{friendshipId:guid}/move-to-default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // 其他错误码由DoAction约定和HandleResult覆盖
    public async Task<IActionResult> MoveFriendToDefaultGroup(Guid friendshipId)
    {
        if (friendshipId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, 
                "Friendship ID cannot be empty.", 
                errorCode: "Validation.GuidEmpty"));
        }

        _logger.LogInformation("User {CurrentUserId} attempting to move friend (FriendshipId: {FriendshipId}) to default group.", CurrentUserId, friendshipId);

        try
        {
            var command = new MoveFriendToDefaultGroupCommand(CurrentUserId, friendshipId);
            var result = await _mediator.Send(command);

            return HandleResult(result, () => NoContent());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid arguments for MoveFriendToDefaultGroup command for user {CurrentUserId}: {ErrorMessage}", CurrentUserId, ex.Message);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Validation.ArgumentError"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving friend (FriendshipId: {FriendshipId}) to default group for user {CurrentUserId}.",
                friendshipId, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiErrorResponse(StatusCodes.Status500InternalServerError, 
                    "An error occurred while moving the friend to the default group.", 
                    errorCode: "FriendGroup.MoveToDefault.UnexpectedError"));
        }
    }
}