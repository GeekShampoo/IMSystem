using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Server.Core.Features.Messages.Commands;
using IMSystem.Server.Core.Features.Messages.Queries;
using IMSystem.Protocol.Enums;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.Messages;
using IMSystem.Protocol.DTOs.Responses.Messages;
using IMSystem.Server.Web.Common;

namespace IMSystem.Server.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 通常查看消息需要授权
    public class MessagesController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMediator mediator, IMapper mapper, ILogger<MessagesController> logger)
        {
            _mediator = mediator;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前用户与另一用户的单聊历史消息。
        /// </summary>
        /// <param name="otherUserId">聊天对象的ID。</param>
        /// <param name="pageNumber">页码 (从1开始)，默认为1。</param>
        /// <param name="pageSize">每页消息数量，默认为20，最大为100。</param>
        /// <returns>分页的消息列表。</returns>
        [HttpGet("user/{otherUserId:guid}")]
        [ProducesResponseType(typeof(PagedResult<MessageDto>), StatusCodes.Status200OK)]
        // 其他错误码由GetPaged约定和HandleResult覆盖
        public async Task<IActionResult> GetUserMessages(
            Guid otherUserId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (otherUserId == Guid.Empty)
            {
                _logger.LogWarning("GetUserMessages 请求中的 otherUserId 为空。");
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "聊天对象ID (otherUserId) 不能为空", errorCode: "Validation.GuidEmpty"));
            }
            
            if (otherUserId == CurrentUserId)
            {
                _logger.LogWarning("GetUserMessages 请求中 otherUserId ({OtherUserId}) 不能与当前用户ID ({CurrentUserId}) 相同。", otherUserId, CurrentUserId);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "不能查询与自己的聊天记录", errorCode: "Validation.SameUser"));
            }

            _logger.LogInformation(
                "用户 {CurrentUserId} 请求与用户 {OtherUserId} 的聊天记录。PageNumber: {PageNumber}, PageSize: {PageSize}",
                CurrentUserId, otherUserId, pageNumber, pageSize);

            try
            {
                var query = new GetUserMessagesQuery(CurrentUserId, otherUserId, pageNumber, pageSize);
                var result = await _mediator.Send(query);

                if (!result.IsSuccess)
                {
                    // 处理失败，例如记录日志并返回适当的状态码
                    _logger.LogWarning("获取用户 {CurrentUserId} 与用户 {OtherUserId} 的聊天记录失败: {Error}",
                        CurrentUserId, otherUserId, result.Error);
                    
                    if (result.Error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, result.Error.Message, errorCode: result.Error.Code));
                    }
                    else if (result.Error.Code.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) || 
                             result.Error.Code.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
                    {
                        return Forbid(result.Error.Message);
                    }
                    else
                    {
                        return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, result.Error.Message, errorCode: result.Error.Code));
                    }
                }
                return Ok(result.Value);
            }
            catch (ArgumentOutOfRangeException ex) // 来自查询构造函数的 limit 参数范围错误
            {
                _logger.LogWarning("获取用户消息参数范围错误: {ErrorMessage}", ex.Message);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Validation.ArgumentOutOfRange"));
            }
            catch (ArgumentException ex) // 来自查询构造函数的其他参数错误
            {
                _logger.LogWarning("获取用户消息参数错误: {ErrorMessage}", ex.Message);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Validation.ArgumentError"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为用户 {CurrentUserId} 获取与用户 {OtherUserId} 的聊天记录过程中发生意外错误。", CurrentUserId, otherUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "获取聊天记录过程中发生内部错误，请稍后重试", errorCode: "Server.UnexpectedError"));
            }
        }

        /// <summary>
        /// 获取指定群组的聊天历史消息。
        /// </summary>
        /// <param name="groupId">群组的ID。</param>
        /// <param name="pageNumber">页码 (从1开始)，默认为1。</param>
        /// <param name="pageSize">每页消息数量，默认为20，最大为100。</param>
        /// <returns>分页的消息列表。</returns>
        [HttpGet("group/{groupId:guid}")]
        [ProducesResponseType(typeof(PagedResult<MessageDto>), StatusCodes.Status200OK)]
        // 其他错误码由GetPaged约定和HandleResult覆盖
        public async Task<IActionResult> GetGroupMessages(
            Guid groupId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (groupId == Guid.Empty)
            {
                _logger.LogWarning("GetGroupMessages 请求中的 groupId 为空。");
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "群组ID (groupId) 不能为空", errorCode: "Validation.GuidEmpty"));
            }

            _logger.LogInformation(
                "用户 {CurrentUserId} 请求群组 {GroupId} 的聊天记录。PageNumber: {PageNumber}, PageSize: {PageSize}",
                CurrentUserId, groupId, pageNumber, pageSize);

            try
            {
                var query = new GetGroupMessagesQuery(CurrentUserId, groupId, pageNumber, pageSize);
                var result = await _mediator.Send(query);
                
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("获取群组 {GroupId} 消息失败: Code={ErrorCode}, Message={ErrorMessage}", groupId, result.Error.Code, result.Error.Message);
                    
                    if (result.Error.Code.Equals("Group.NotFound", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, result.Error.Message, errorCode: result.Error.Code));
                    }
                    
                    if (result.Error.Code.Equals("Message.AccessDenied", StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, 
                            new ApiErrorResponse(StatusCodes.Status403Forbidden, result.Error.Message, errorCode: result.Error.Code));
                    }
                    
                    return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, result.Error.Message, errorCode: result.Error.Code));
                }
                return Ok(result.Value);
            }
            catch (ArgumentOutOfRangeException ex) // 来自查询构造函数的 limit 参数范围错误
            {
                _logger.LogWarning("获取群组消息参数范围错误: {ErrorMessage}", ex.Message);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Validation.ArgumentOutOfRange"));
            }
            catch (ArgumentException ex) // 来自查询构造函数的其他参数错误
            {
                _logger.LogWarning("获取群组消息参数错误: {ErrorMessage}", ex.Message);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, ex.Message, errorCode: "Validation.ArgumentError"));
            }
            catch (UnauthorizedAccessException ex) // 来自 GetGroupMessagesQueryHandler，如果用户不在群组中
            {
                _logger.LogWarning("用户 {CurrentUserId} 无权访问群组 {GroupId} 的消息: {ErrorMessage}", CurrentUserId, groupId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, 
                    new ApiErrorResponse(StatusCodes.Status403Forbidden, ex.Message, errorCode: "Message.AccessDenied"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为用户 {CurrentUserId} 获取群组 {GroupId} 的聊天记录过程中发生意外错误。", CurrentUserId, groupId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "获取群组聊天记录过程中发生内部错误，请稍后重试", errorCode: "Server.UnexpectedError"));
            }
        }

        /// <summary>
        /// 获取指定群消息的已读成员列表。
        /// </summary>
        /// <param name="messageId">群消息的ID。</param>
        /// <returns>已读该消息的成员列表。</returns>
        [HttpGet("group/{messageId:guid}/readby")]
        [ProducesResponseType(typeof(Result<GetGroupMessageReadUsersResponse>), StatusCodes.Status200OK)]
        // 其他错误码由GetById约定和直接状态码处理覆盖
        public async Task<IActionResult> GetGroupMessageReadUsers(Guid messageId)
        {
            if (messageId == Guid.Empty)
            {
                _logger.LogWarning("GetGroupMessageReadUsers 请求中的 messageId 为空。");
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "消息ID (messageId) 不能为空", errorCode: "Validation.GuidEmpty"));
            }

            _logger.LogInformation("用户 {CurrentUserId} 请求消息 {MessageId} 的已读成员列表。", CurrentUserId, messageId);

            try
            {
                var query = new GetGroupMessageReadUsersQuery(messageId, CurrentUserId);
                var result = await _mediator.Send(query);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("获取消息 {MessageId} 已读成员列表失败: Code={ErrorCode}, Message={ErrorMessage}",
                        messageId, result.Error.Code, result.Error.Message);

                    int statusCode;
                    string errorCode;

                    switch (result.Error.Code)
                    {
                        case "MessageNotFound":
                            statusCode = StatusCodes.Status404NotFound;
                            errorCode = "ResourceNotFound";
                            break;
                        case "NotAGroupMessage":
                            statusCode = StatusCodes.Status400BadRequest;
                            errorCode = "ValidationFailed";
                            break;
                        case "UserNotGroupMember":
                            statusCode = StatusCodes.Status403Forbidden;
                            errorCode = "OperationForbidden";
                            break;
                        default:
                            statusCode = StatusCodes.Status400BadRequest;
                            errorCode = "ValidationFailed";
                            break;
                    }

                    return StatusCode(statusCode, 
                        new ApiErrorResponse(statusCode, result.Error.Message, errorCode: errorCode));
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为用户 {CurrentUserId} 获取消息 {MessageId} 的已读成员列表过程中发生意外错误。", CurrentUserId, messageId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "获取已读成员列表过程中发生内部错误，请稍后重试", errorCode: "Server.UnexpectedError"));
            }
        }

        /// <summary>
        /// 发送单聊消息。
        /// </summary>
        /// <param name="request">发送消息的请求体。</param>
        /// <returns>操作结果。</returns>
        [HttpPost("user")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)] // 保留，因为有特殊的冲突处理
        // 其他错误码由DoAction约定和HandleResult覆盖
        public async Task<IActionResult> SendMessageToUser([FromBody] SendMessageDto request)
        {
            if (request.RecipientId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "接收者ID不能为空", errorCode: "Validation.GuidEmpty"));
            }

            if (request.RecipientType != ProtocolMessageRecipientType.User)
            {
                _logger.LogWarning("SendMessageToUser 接收到无效的 RecipientType: {RecipientType}，期望为 User。", request.RecipientType);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "RecipientType无效", 
                    detail: "此端点仅用于发送用户消息，RecipientType 必须是 'User'。", errorCode: "Validation.InvalidRecipientType"));
            }

            if (CurrentUserId == request.RecipientId)
            {
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "不能给自己发送消息", errorCode: "Validation.CannotMessageSelf"));
            }

            _logger.LogInformation("用户 {CurrentUserId} 尝试向用户 {RecipientId} 发送类型为 {MessageType} 的消息。ClientMessageId: {ClientMessageId}, ReplyToMessageId: {ReplyToMessageId}",
                CurrentUserId, request.RecipientId, request.MessageType, request.ClientMessageId, request.ReplyToMessageId);

            try
            {
                var command = new SendMessageCommand
                {
                    SenderId = CurrentUserId,
                    RecipientId = request.RecipientId,
                    RecipientType = ProtocolMessageRecipientType.User.ToString(),
                    Content = request.Content,
                    MessageType = request.MessageType.ToString(),
                    ClientMessageId = request.ClientMessageId,
                    ReplyToMessageId = request.ReplyToMessageId
                };

                var result = await _mediator.Send(command);
                return HandleResult(result, value => Ok(Result<Guid>.Success(value)));
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "用户 {CurrentUserId} 发送消息时发生并发冲突。", CurrentUserId);
                return Conflict(new ApiErrorResponse(StatusCodes.Status409Conflict, "发送消息操作冲突", 
                    detail: "发送消息时发生并发冲突，请重试。", errorCode: "Concurrency.Conflict"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户 {CurrentUserId} 向用户 {RecipientId} 发送消息时发生意外错误。", CurrentUserId, request.RecipientId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "发送消息过程中发生内部错误", 
                    detail: "发送消息过程中发生内部错误，请稍后重试。", errorCode: "Server.UnexpectedError"));
            }
        }

        /// <summary>
        /// 发送群聊消息。
        /// </summary>
        /// <param name="request">发送消息的请求体。</param>
        /// <returns>操作结果。</returns>
        [HttpPost("group")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)] // 保留，因为有特殊的冲突处理
        // 其他错误码由DoAction约定和HandleResult覆盖
        public async Task<IActionResult> SendMessageToGroup([FromBody] SendMessageDto request)
        {
            if (request.RecipientId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "群组ID不能为空", errorCode: "Validation.GuidEmpty"));
            }

            if (request.RecipientType != ProtocolMessageRecipientType.Group)
            {
                _logger.LogWarning("SendMessageToGroup 接收到无效的 RecipientType: {RecipientType}，期望为 Group。", request.RecipientType);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "RecipientType无效", 
                    detail: "此端点仅用于发送群组消息，RecipientType 必须是 'Group'。", errorCode: "Validation.InvalidRecipientType"));
            }

            _logger.LogInformation("用户 {CurrentUserId} 尝试向群组 {RecipientId} 发送类型为 {MessageType} 的消息。ClientMessageId: {ClientMessageId}, ReplyToMessageId: {ReplyToMessageId}",
                CurrentUserId, request.RecipientId, request.MessageType, request.ClientMessageId, request.ReplyToMessageId);

            try
            {
                var command = new SendMessageCommand
                {
                    SenderId = CurrentUserId,
                    RecipientId = request.RecipientId,
                    RecipientType = ProtocolMessageRecipientType.Group.ToString(),
                    Content = request.Content,
                    MessageType = request.MessageType.ToString(),
                    ClientMessageId = request.ClientMessageId,
                    ReplyToMessageId = request.ReplyToMessageId
                };

                var result = await _mediator.Send(command);
                
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("发送群聊消息失败: {Error}", result.Error);
                    if (result.Error.Message.Contains("not a member", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("Forbidden.NotMember", StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, 
                            new ApiErrorResponse(StatusCodes.Status403Forbidden, "您不是该群组成员，无法发送消息", errorCode: "OperationForbidden"));
                    }
                    if (result.Error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("NotFound.Group", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, "群组未找到", 
                            detail: result.Error.Message, errorCode: "ResourceNotFound"));
                    }
                    return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "发送群聊消息失败", 
                        detail: result.Error.Message, errorCode: "ValidationFailed"));
                }
                
                return Ok(Result<Guid>.Success(result.Value));
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "用户 {CurrentUserId} 发送群聊消息时发生并发冲突。", CurrentUserId);
                return Conflict(new ApiErrorResponse(StatusCodes.Status409Conflict, "发送群聊消息操作冲突", 
                    detail: "发送群聊消息时发生并发冲突，请重试。", errorCode: "Concurrency.Conflict"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户 {CurrentUserId} 向群组 {RecipientId} 发送消息时发生意外错误。", CurrentUserId, request.RecipientId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "发送消息过程中发生内部错误", 
                    detail: "发送消息过程中发生内部错误，请稍后重试。", errorCode: "Server.UnexpectedError"));
            }
        }

        /// <summary>
        /// 标记消息为已读。
        /// </summary>
        /// <param name="request">标记已读的请求体。</param>
        /// <returns>操作结果。</returns>
        [HttpPost("read")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        // 其他错误码由DoAction约定和HandleResult覆盖
        public async Task<IActionResult> MarkMessagesAsRead([FromBody] MarkMessagesAsReadRequest request)
        {
            if (request.ChatId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "ChatId 不能为空", errorCode: "Validation.GuidEmpty"));
            }

            Guid? chatPartnerIdForCommand = request.ChatType == ProtocolChatType.Private ? request.ChatId : null;
            Guid? groupIdForCommand = request.ChatType == ProtocolChatType.Group ? request.ChatId : null;

            _logger.LogInformation("用户 {CurrentUserId} 尝试标记 ChatId {ChatId} (Type: {ChatType}) 的消息为已读。UpToMessageId: {UpToMessageId}, LastReadTimestamp: {LastReadTimestamp}",
                CurrentUserId, request.ChatId, request.ChatType.ToString(), request.UpToMessageId, request.LastReadTimestamp);
            
            try
            {
                var command = new MarkMessageAsReadCommand(
                    readerUserId: CurrentUserId,
                    chatPartnerId: chatPartnerIdForCommand,
                    groupId: groupIdForCommand,
                    upToMessageId: request.UpToMessageId,
                    lastReadTimestamp: request.LastReadTimestamp
                );

                var result = await _mediator.Send(command);
                return HandleResult(result, () => Ok());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户 {CurrentUserId} 标记 ChatId {ChatId} (Type: {ChatType}) 消息已读时发生意外错误。", CurrentUserId, request.ChatId, request.ChatType);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "标记消息已读过程中发生内部错误", 
                    detail: "标记消息已读过程中发生内部错误，请稍后重试。", errorCode: "Server.UnexpectedError"));
            }
        }

        /// <summary>
        /// 撤回先前发送的消息。
        /// </summary>
        /// <param name="messageId">要撤回的消息的 ID。</param>
        /// <returns>如果成功则无内容，否则返回错误响应。</returns>
        [HttpPost("{messageId:guid}/recall")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        // 其他错误码由DoAction约定和直接状态码处理覆盖
        public async Task<IActionResult> RecallMessage(Guid messageId)
        {
            if (messageId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "消息ID不能为空", errorCode: "Validation.GuidEmpty"));
            }

            _logger.LogInformation("用户 {ActorUserId} 尝试撤回消息 {MessageId}。", CurrentUserId, messageId);

            try
            {
                var command = new RecallMessageCommand(messageId, CurrentUserId);
                var result = await _mediator.Send(command);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("用户 {ActorUserId} 撤回消息 {MessageId} 失败: {Error}",
                        messageId, CurrentUserId, result.Error);
                    
                    if (result.Error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("NotFound.Message", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, "消息未找到", 
                            detail: result.Error.Message, errorCode: "ResourceNotFound"));
                    }
                    if (result.Error.Message.Contains("only recall messages you sent", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("Forbidden.NotSender", StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, 
                            new ApiErrorResponse(StatusCodes.Status403Forbidden, "您只能撤回自己发送的消息", 
                            detail: result.Error.Message, errorCode: "OperationForbidden"));
                    }
                    if (result.Error.Message.Contains("time limit exceeded", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("Validation.RecallTimeLimitExceeded", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Message.Contains("cannot be recalled", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("Validation.MessageNotRecallable", StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "消息撤回失败", 
                            detail: result.Error.Message, errorCode: "ValidationFailed"));
                    }

                    return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "消息撤回失败", 
                        detail: result.Error.Message, errorCode: "ValidationFailed"));
                }

                _logger.LogInformation("用户 {ActorUserId} 成功撤回消息 {MessageId}。", CurrentUserId, messageId);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("撤回消息 {MessageId} 的命令参数无效: {ErrorMessage}", messageId, ex.Message);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "参数无效", 
                    detail: ex.Message, errorCode: "ValidationFailed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户 {ActorUserId} 撤回消息 {MessageId} 时发生意外错误。", CurrentUserId, messageId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "撤回消息过程中发生内部错误", 
                    detail: "撤回消息过程中发生内部错误，请稍后重试。", errorCode: "Server.UnexpectedError"));
            }
        }

        /// <summary>
        /// 编辑已有消息。
        /// </summary>
        /// <param name="messageId">要编辑的消息 ID。</param>
        /// <param name="request">包含消息新内容的请求。</param>
        /// <returns>表示操作结果的 IActionResult。</returns>
        [HttpPut("{messageId:guid}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        // 其他错误码由Put约定和直接状态码处理覆盖
        public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequest request)
        {
            if (messageId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "消息ID不能为空", errorCode: "Validation.GuidEmpty"));
            }

            if (request.MessageId != messageId)
            {
                _logger.LogWarning("EditMessage 请求中路由中的消息ID ({RouteMessageId}) 与请求体中的消息ID ({BodyMessageId}) 不匹配。", messageId, request.MessageId);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "URL 中的消息ID与请求体中的必须匹配", errorCode: "Validation.IdMismatch"));
            }

            _logger.LogInformation("用户 {UserId} 尝试编辑消息 {MessageId} 的内容。", CurrentUserId, messageId);

            try
            {
                var command = new EditMessageCommand
                {
                    MessageId = messageId,
                    NewContent = request.Content,
                    UserId = CurrentUserId
                };

                var result = await _mediator.Send(command);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("用户 {UserId} 编辑消息 {MessageId} 失败: Code={ErrorCode}, Message={ErrorMessage}",
                        messageId, CurrentUserId, result.Error.Code, result.Error.Message);

                    switch (result.Error.Code)
                    {
                        case "Message.NotFound":
                            return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, "消息未找到", 
                                detail: result.Error.Message, errorCode: "ResourceNotFound"));
                            
                        case "Message.Forbidden":
                            return StatusCode(StatusCodes.Status403Forbidden, 
                                new ApiErrorResponse(StatusCodes.Status403Forbidden, "您无权编辑此消息", 
                                detail: result.Error.Message, errorCode: "OperationForbidden"));
                            
                        case "Message.EditTimeExpired":
                            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "消息编辑时间已过期", 
                                detail: result.Error.Message, errorCode: "ValidationFailed"));
                            
                        default:
                            return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "消息编辑失败", 
                                detail: result.Error.Message, errorCode: "ValidationFailed"));
                    }
                }

                _logger.LogInformation("用户 {UserId} 成功编辑消息 {MessageId}。", CurrentUserId, messageId);
                return Ok(result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning(ex, "用户 {UserId} 编辑消息 {MessageId} 时发生验证错误。", messageId, CurrentUserId);
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "输入验证失败", 
                    detail: ex.Errors.FirstOrDefault()?.ErrorMessage ?? "提供的数据无效。", errorCode: "ValidationFailed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户 {UserId} 编辑消息 {MessageId} 时发生意外错误。", messageId, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "编辑消息过程中发生内部错误", 
                    detail: "编辑消息过程中发生内部错误，请稍后重试。", errorCode: "Server.UnexpectedError"));
            }
        }

        /// <summary>
        /// 发送端到端加密消息。
        /// </summary>
        /// <param name="request">发送加密消息的请求体。</param>
        /// <returns>操作结果，包含消息ID。</returns>
        [HttpPost("encrypted")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        // 其他错误码由DoAction约定和直接状态码处理覆盖
        public async Task<IActionResult> SendEncryptedMessage([FromBody] SendEncryptedMessageRequest request)
        {
            if (string.IsNullOrEmpty(request.RecipientId))
            {
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "接收者ID不能为空", errorCode: "Validation.EmptyRecipientId"));
            }

            _logger.LogInformation("用户 {CurrentUserId} 尝试向 {RecipientId} (类型: {ChatType}) 发送加密消息。",
                CurrentUserId, request.RecipientId, request.ChatType);

            try
            {
                var command = new SendEncryptedMessageCommand
                {
                    SenderUserId = CurrentUserId,
                    RecipientId = request.RecipientId,
                    ChatType = request.ChatType,
                    EncryptedContent = request.EncryptedContent
                };

                var result = await _mediator.Send(command);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("发送加密消息失败: {Error}", result.Error);
                    
                    if (result.Error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, "接收者未找到", 
                            detail: result.Error.Message, errorCode: "ResourceNotFound"));
                    }
                    
                    if (result.Error.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Message.Contains("not a member", StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, 
                            new ApiErrorResponse(StatusCodes.Status403Forbidden, "您无权发送此消息", 
                            detail: result.Error.Message, errorCode: "OperationForbidden"));
                    }
                    
                    return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "发送加密消息失败", 
                        detail: result.Error.Message, errorCode: "ValidationFailed"));
                }

                _logger.LogInformation("成功发送加密消息，消息ID: {MessageId}", result.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户 {CurrentUserId} 向 {RecipientId} (类型: {ChatType}) 发送加密消息时发生意外错误。",
                    CurrentUserId, request.RecipientId, request.ChatType);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "发送加密消息过程中发生内部错误", 
                    detail: "发送加密消息过程中发生内部错误，请稍后重试。", errorCode: "Server.UnexpectedError"));
            }
        }

        /// <summary>
        /// 获取指定序列号之后的所有消息。
        /// </summary>
        /// <param name="request">包含聊天类型、接收者ID和序列号的请求对象。</param>
        /// <returns>操作结果，包含消息列表。</returns>
        [HttpPost("after-sequence")]
        [ProducesResponseType(typeof(IEnumerable<MessageDto>), StatusCodes.Status200OK)]
        // 其他错误码由DoAction约定和直接状态码处理覆盖
        public async Task<IActionResult> GetMessagesAfterSequence([FromBody] GetMessagesAfterSequenceRequest request)
        {
            try
            {
                var query = new GetMessagesAfterSequenceQuery
                {
                    UserId = CurrentUserId,
                    RecipientId = request.RecipientId,
                    ChatType = request.ChatType,
                    AfterSequence = request.AfterSequence,
                    Limit = request.Limit ?? 50 // 默认限制为50条消息
                };

                var result = await _mediator.Send(query);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("获取序列号 {Sequence} 之后的消息失败: {Error}", 
                        request.AfterSequence, result.Error);
                    
                    if (result.Error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, "聊天对象未找到", 
                            detail: result.Error.Message, errorCode: "ResourceNotFound"));
                    }
                    
                    if (result.Error.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Code.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) || 
                        result.Error.Message.Contains("not a member", StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, 
                            new ApiErrorResponse(StatusCodes.Status403Forbidden, "您无权访问此聊天", 
                            detail: result.Error.Message, errorCode: "OperationForbidden"));
                    }
                    
                    return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "获取消息失败", 
                        detail: result.Error.Message, errorCode: "ValidationFailed"));
                }

                var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(result.Value);
                return Ok(messageDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户 {CurrentUserId} 获取序列号 {Sequence} 之后的消息时发生意外错误。",
                    CurrentUserId, request.AfterSequence);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ApiErrorResponse(StatusCodes.Status500InternalServerError, "获取消息过程中发生内部错误", 
                    detail: "获取消息过程中发生内部错误，请稍后重试。", errorCode: "Server.UnexpectedError"));
            }
        }
    }
}