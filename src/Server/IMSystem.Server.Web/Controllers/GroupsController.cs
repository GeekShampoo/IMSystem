using AutoMapper;
using IMSystem.Protocol.DTOs.Requests.Groups;
using IMSystem.Protocol.DTOs.Responses.Groups;
using IMSystem.Server.Core.Features.Groups.Commands;
using IMSystem.Server.Core.Features.Groups.Queries; // 用于未来的 GET 端点
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using IMSystem.Protocol.Common;
using IMSystem.Server.Web.Common;

namespace IMSystem.Server.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 所有群组操作都需要授权
public class GroupsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IMediator mediator, IMapper mapper, ILogger<GroupsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 创建一个新的群组。
    /// </summary>
    /// <param name="request">创建群组的请求数据。</param>
    /// <returns>成功则返回201 Created及新群组的ID；失败则返回错误信息。</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)] // 保留，因为有特殊的冲突处理
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var command = _mapper.Map<CreateGroupCommand>(request);
        var commandWithCreator = command with { CreatorUserId = CurrentUserId };

        _logger.LogInformation("用户 {CreatorUserId} 请求创建群组，名称: {GroupName}", CurrentUserId, request.Name);

        try
        {
            var result = await _mediator.Send(commandWithCreator);

            return HandleResult(result, 
                newGroupId => CreatedAtAction(nameof(GetGroupById), new { groupId = newGroupId }, new { groupId = newGroupId }),
                conflictErrorCode: "Group.NameConflict");
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "用户 {CreatorUserId} 创建群组时发生并发冲突。", CurrentUserId);
            return Conflict(new ApiErrorResponse(StatusCodes.Status409Conflict, "创建群组操作冲突")
            {
                Detail = "创建群组时发生并发冲突，请重试。"
            });
        }
    }

    /// <summary>
    /// 根据ID获取群组信息。
    /// </summary>
    /// <param name="groupId">群组ID。</param>
    [HttpGet("{groupId:guid}", Name = "GetGroupById")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupById(Guid groupId)
    {
        _logger.LogInformation("用户 {UserId} 请求获取群组 {GroupId} 的详细信息。", CurrentUserId, groupId);

        var query = new GetGroupDetailsQuery(groupId, CurrentUserId);
        var result = await _mediator.Send(query);

        return HandleResult(result, value => Ok(value));
    }

    /// <summary>
    /// 获取当前用户加入的所有群组。
    /// </summary>
    /// <returns>用户加入的群组列表。</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GroupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserGroups()
    {
        _logger.LogInformation("用户 {UserId} 请求获取其群组列表。", CurrentUserId);

        var query = new GetUserGroupsQuery(CurrentUserId);
        var result = await _mediator.Send(query);

        return HandleResult(result, value => Ok(value));
    }

    /// <summary>
    /// 更新指定群组的详细信息。
    /// </summary>
    /// <param name="groupId">要更新的群组ID。</param>
    /// <param name="request">包含更新信息的请求体。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPut("{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)] // 保留，因为有特殊的冲突处理
    public async Task<IActionResult> UpdateGroupDetails(Guid groupId, [FromBody] UpdateGroupDetailsRequest request)
    {
        _logger.LogInformation("用户 {UserId} 请求更新群组 {GroupId} 的详细信息。Name: {Name}, Desc: {Description}, Avatar: {AvatarUrl}",
            CurrentUserId, groupId, request.Name, request.Description, request.AvatarUrl);

        var command = new UpdateGroupDetailsCommand(
            groupId,
            request.Name,
            request.Description,
            request.AvatarUrl,
            CurrentUserId
        );

        var result = await _mediator.Send(command);

        return HandleResult(result, 
            () => NoContent(), 
            conflictErrorCode: "Group.NameConflict");
    }

    /// <summary>
    /// 接受一个群组邀请。
    /// </summary>
    /// <param name="invitationId">要接受的群组邀请ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPost("invitations/{invitationId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcceptInvitation(Guid invitationId)
    {
        _logger.LogInformation("用户 {UserId} 尝试接受群组邀请 {InvitationId}。", CurrentUserId, invitationId);

        var command = new AcceptGroupInvitationCommand(invitationId, CurrentUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 拒绝一个群组邀请。
    /// </summary>
    /// <param name="invitationId">要拒绝的群组邀请ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPost("invitations/{invitationId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RejectInvitation(Guid invitationId)
    {
        _logger.LogInformation("用户 {UserId} 尝试拒绝群组邀请 {InvitationId}。", CurrentUserId, invitationId);

        var command = new RejectGroupInvitationCommand(invitationId, CurrentUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 获取当前用户收到的待处理群组邀请列表。
    /// </summary>
    /// <returns>待处理的群组邀请列表。</returns>
    [HttpGet("invitations/pending")]
    [ProducesResponseType(typeof(IEnumerable<GroupInvitationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingInvitations()
    {
        _logger.LogInformation("用户 {UserId} 请求获取其待处理的群组邀请列表。", CurrentUserId);

        var query = new GetPendingGroupInvitationsQuery(CurrentUserId);
        var result = await _mediator.Send(query);

        return HandleResult(result, value => Ok(value));
    }

    /// <summary>
    /// 邀请用户加入指定群组。
    /// </summary>
    /// <param name="groupId">目标群组的ID。</param>
    /// <param name="request">包含被邀请用户ID和可选消息的请求体。</param>
    /// <returns>成功则返回201 Created及新邀请的ID；失败则返回错误信息。</returns>
    [HttpPost("{groupId:guid}/invitations")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)] // 保留，因为有特殊的冲突处理
    public async Task<IActionResult> InviteUserToGroup(Guid groupId, [FromBody] InviteUserToGroupRequest request)
    {
        _logger.LogInformation("用户 {InviterId} 尝试邀请用户 {InvitedUserId} 加入群组 {GroupId}。消息: {Message}, 过期时间(小时): {ExpiresInHours}",
            CurrentUserId, request.InvitedUserId, groupId, request.Message, request.ExpiresInHours);

        DateTime? expiresAt = request.ExpiresInHours.HasValue
            ? DateTime.UtcNow.AddHours(request.ExpiresInHours.Value)
            : null; // 或系统默认过期时间

        var command = new InviteUserToGroupCommand(
            groupId,
            CurrentUserId,
            request.InvitedUserId,
            request.Message,
            expiresAt
        );

        var result = await _mediator.Send(command);

        return HandleResult(result, 
            newInvitationId => CreatedAtAction(null, new { invitationId = newInvitationId }, new { invitationId = newInvitationId }),
            conflictErrorCode: "Group.Invite.PendingExists");
    }

    /// <summary>
    /// 允许当前认证用户离开指定的群组。
    /// </summary>
    /// <param name="groupId">要离开的群组ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPost("{groupId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveGroup(Guid groupId)
    {
        _logger.LogInformation("用户 {UserId} 尝试离开群组 {GroupId}。", CurrentUserId, groupId);

        var command = new LeaveGroupCommand(groupId, CurrentUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 转让指定群组的群主身份给另一位成员。
    /// </summary>
    /// <param name="groupId">要转让群主身份的群组ID。</param>
    /// <param name="request">包含新群主用户ID的请求体。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPost("{groupId:guid}/transfer-ownership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TransferOwnership(Guid groupId, [FromBody] TransferGroupOwnershipRequest request)
    {
        _logger.LogInformation("用户 {CurrentUserId} 尝试将群组 {GroupId} 的群主身份转让给用户 {NewOwnerUserId}。",
            CurrentUserId, groupId, request.NewOwnerUserId);

        var command = new TransferGroupOwnershipCommand(groupId, CurrentUserId, request.NewOwnerUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 将指定群组成员提升为管理员。
    /// </summary>
    /// <param name="groupId">群组ID。</param>
    /// <param name="memberUserId">要提升的成员的用户ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPost("{groupId:guid}/members/{memberUserId:guid}/promote-admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PromoteToAdmin(Guid groupId, Guid memberUserId)
    {
        if (CurrentUserId == memberUserId)
        {
            _logger.LogWarning("用户 {ActorUserId} 尝试提升自己为管理员 (PromoteToAdmin)。", CurrentUserId);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "不能提升自己为管理员。"));
        }

        _logger.LogInformation("用户 {ActorUserId} 尝试将群组 {GroupId} 的成员 {MemberUserId} 提升为管理员。",
            CurrentUserId, groupId, memberUserId);

        var command = new PromoteToAdminCommand(groupId, CurrentUserId, memberUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 将指定群组管理员降级为普通成员。
    /// </summary>
    /// <param name="groupId">群组ID。</param>
    /// <param name="memberUserId">要降级的管理员的用户ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPost("{groupId:guid}/members/{memberUserId:guid}/demote-admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DemoteToMember(Guid groupId, Guid memberUserId)
    {
        if (CurrentUserId == memberUserId)
        {
            _logger.LogWarning("用户 {ActorUserId} 尝试降级自己 (DemoteToMember)。", CurrentUserId);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "不能降级自己。"));
        }

        _logger.LogInformation("用户 {ActorUserId} 尝试将群组 {GroupId} 的管理员 {MemberUserId} 降级为普通成员。",
            CurrentUserId, groupId, memberUserId);

        var command = new DemoteAdminCommand(groupId, CurrentUserId, memberUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 删除指定的群组。
    /// </summary>
    /// <remarks>
    /// 只有群主才能删除群组。删除群组将移除所有成员和相关数据。
    /// </remarks>
    /// <param name="groupId">要删除的群组ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpDelete("{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGroup(Guid groupId)
    {
        _logger.LogInformation("用户 {ActorUserId} 尝试删除群组 {GroupId}。", CurrentUserId, groupId);

        var command = new DisbandGroupCommand(groupId, CurrentUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 取消一个群组邀请。
    /// </summary>
    /// <remarks>
    /// 只有邀请的发送者或群组的管理员/群主可以取消邀请。
    /// </remarks>
    /// <param name="invitationId">要取消的群组邀请ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpPost("invitations/{invitationId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelInvitation(Guid invitationId)
    {
        _logger.LogInformation("用户 {CancellerUserId} 尝试取消群组邀请 {InvitationId}。", CurrentUserId, invitationId);

        var command = new CancelGroupInvitationCommand(invitationId, CurrentUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 获取指定群组已发送的所有邀请列表。
    /// </summary>
    /// <remarks>
    /// 只有群组的管理员或群主可以查看此列表。
    /// </remarks>
    /// <param name="groupId">要查询的群组ID。</param>
    /// <returns>该群组已发送的邀请列表。</returns>
    [HttpGet("{groupId:guid}/invitations/sent")]
    [ProducesResponseType(typeof(IEnumerable<GroupInvitationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSentInvitationsForGroup(Guid groupId)
    {
        _logger.LogInformation("用户 {RequestorUserId} 请求获取群组 {GroupId} 已发送的邀请列表。", CurrentUserId, groupId);

        var query = new GetSentGroupInvitationsQuery(groupId, CurrentUserId);
        var result = await _mediator.Send(query);

        return HandleResult(result, value => Ok(value));
    }

    /// <summary>
    /// 从群组中踢出一名成员。
    /// </summary>
    /// <remarks>
    /// 只有群组的管理员或群主有权限踢出成员。
    /// 群主不能被踢出。管理员不能踢出其他管理员或群主。
    /// </remarks>
    /// <param name="groupId">群组ID。</param>
    /// <param name="memberUserId">要踢出的成员的用户ID。</param>
    /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
    [HttpDelete("{groupId:guid}/members/{memberUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> KickGroupMember(Guid groupId, Guid memberUserId)
    {
        _logger.LogInformation("用户 {ActorUserId} 尝试从群组 {GroupId} 中踢出成员 {MemberUserIdToKick}。",
            CurrentUserId, groupId, memberUserId);

        var command = new KickGroupMemberCommand(groupId, memberUserId, CurrentUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }

    /// <summary>
    /// 设置或更新特定群组的公告。
    /// </summary>
    /// <param name="groupId">群组的 ID。</param>
    /// <param name="request">包含新公告的请求。</param>
    /// <returns>如果成功则无内容，否则返回错误响应。</returns>
    [HttpPut("{groupId:guid}/announcement")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetGroupAnnouncement(Guid groupId, [FromBody] SetGroupAnnouncementRequest request)
    {
        _logger.LogInformation("User {ActorUserId} attempting to set announcement for group {GroupId}.", CurrentUserId, groupId);

        try
        {
            var command = new SetGroupAnnouncementCommand(groupId, CurrentUserId, request.Announcement);
            var result = await _mediator.Send(command);

            return HandleResult(result, () => NoContent());
        }
        catch (ArgumentException ex) // 来自命令构造函数
        {
            _logger.LogWarning("Invalid arguments for SetGroupAnnouncement command for group {GroupId}: {ErrorMessage}", groupId, ex.Message);
            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message));
        }
    }
}