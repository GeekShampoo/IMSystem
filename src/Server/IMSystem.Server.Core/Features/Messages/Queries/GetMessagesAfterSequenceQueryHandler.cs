using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.Enums;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Messages.Queries
{
    /// <summary>
    /// 获取指定序列号之后的消息查询处理程序
    /// </summary>
    public class GetMessagesAfterSequenceQueryHandler : IRequestHandler<GetMessagesAfterSequenceQuery, Result<IEnumerable<MessageDto>>>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetMessagesAfterSequenceQueryHandler> _logger;

        public GetMessagesAfterSequenceQueryHandler(
            IMessageRepository messageRepository,
            IUserRepository userRepository,
            IGroupRepository groupRepository,
            IGroupMemberRepository groupMemberRepository,
            IFriendshipRepository friendshipRepository,
            IMapper mapper,
            ILogger<GetMessagesAfterSequenceQueryHandler> logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
            _groupMemberRepository = groupMemberRepository ?? throw new ArgumentNullException(nameof(groupMemberRepository));
            _friendshipRepository = friendshipRepository ?? throw new ArgumentNullException(nameof(friendshipRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IEnumerable<MessageDto>>> Handle(GetMessagesAfterSequenceQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("处理获取序列号 {AfterSequence} 之后的消息请求。用户ID: {UserId}, 聊天类型: {ChatType}, 接收者ID: {RecipientId}, 限制: {Limit}",
                    request.AfterSequence, request.UserId, request.ChatType, request.RecipientId, request.Limit);

                // 验证参数
                if (request.UserId == Guid.Empty)
                {
                    return Result.Failure<IEnumerable<MessageDto>>(new Error("Validation.UserIdRequired", "用户ID不能为空"));
                }

                if (request.RecipientId == Guid.Empty)
                {
                    return Result.Failure<IEnumerable<MessageDto>>(new Error("Validation.RecipientIdRequired", "接收者ID不能为空"));
                }

                // 根据聊天类型执行不同的验证和查询
                if (request.ChatType == ProtocolChatType.Private)
                {
                    // 验证用户是否存在且是好友关系
                    var otherUser = await _userRepository.GetByIdAsync(request.RecipientId);
                    if (otherUser == null)
                    {
                        return Result.Failure<IEnumerable<MessageDto>>(new Error("NotFound.User", "指定的用户不存在"));
                    }

                    // 检查好友关系
                    var friendship = await _friendshipRepository.GetFriendshipAsync(request.UserId, request.RecipientId);
                    if (friendship == null || !friendship.IsConfirmed())
                    {
                        return Result.Failure<IEnumerable<MessageDto>>(new Error("Forbidden.NotFriends", "您与该用户不是好友关系，无法查看消息"));
                    }

                    // 获取私聊消息
                    var messages = await _messageRepository.GetMessagesAfterSequenceAsync(
                        request.UserId, 
                        request.RecipientId, 
                        null, // 私聊没有群组ID
                        request.AfterSequence, 
                        request.Limit);

                    var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
                    
                    _logger.LogInformation("用户 {UserId} 成功获取与用户 {OtherUserId} 的 {Count} 条消息 (序列号 > {AfterSequence})",
                        request.UserId, request.RecipientId, messageDtos.Count(), request.AfterSequence);
                    
                    return Result<IEnumerable<MessageDto>>.Success(messageDtos);
                }
                else if (request.ChatType == ProtocolChatType.Group)
                {
                    // 验证群组是否存在
                    var group = await _groupRepository.GetByIdAsync(request.RecipientId);
                    if (group == null)
                    {
                        return Result.Failure<IEnumerable<MessageDto>>(new Error("NotFound.Group", "指定的群组不存在"));
                    }

                    // 验证用户是否是群成员
                    var membership = await _groupMemberRepository.GetMembershipAsync(request.RecipientId, request.UserId);
                    if (membership == null)
                    {
                        return Result.Failure<IEnumerable<MessageDto>>(new Error("Forbidden.NotGroupMember", "您不是该群组的成员，无法查看消息"));
                    }

                    // 获取群组消息
                    var messages = await _messageRepository.GetMessagesAfterSequenceAsync(
                        null, // 群聊没有特定发送者ID限制
                        null, // 群聊没有特定接收者ID限制
                        request.RecipientId, // 群组ID
                        request.AfterSequence,
                        request.Limit);

                    var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
                    
                    _logger.LogInformation("用户 {UserId} 成功获取群组 {GroupId} 的 {Count} 条消息 (序列号 > {AfterSequence})",
                        request.UserId, request.RecipientId, messageDtos.Count(), request.AfterSequence);
                    
                    return Result<IEnumerable<MessageDto>>.Success(messageDtos);
                }
                else
                {
                    // 不支持的聊天类型
                    return Result.Failure<IEnumerable<MessageDto>>(new Error("Validation.UnsupportedChatType", $"不支持的聊天类型: {request.ChatType}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取序列号 {AfterSequence} 之后的消息过程中发生异常", request.AfterSequence);
                return Result.Failure<IEnumerable<MessageDto>>(new Error("Server.Error", "获取消息过程中发生服务器错误"));
            }
        }
    }
}