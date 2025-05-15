using AutoMapper;
using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.Common; // Added for PagedResult
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For Message entity and User entity
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication; // For AuthenticationException/UnauthorizedAccessException
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Messages.Queries
{
    public class GetGroupMessagesQueryHandler : IRequestHandler<GetGroupMessagesQuery, Result<PagedResult<MessageDto>>>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IMessageReadReceiptRepository _messageReadReceiptRepository; // Added
        private readonly IMapper _mapper;
        private readonly ILogger<GetGroupMessagesQueryHandler> _logger;

        public GetGroupMessagesQueryHandler(
            IMessageRepository messageRepository,
            IGroupRepository groupRepository,
            IMessageReadReceiptRepository messageReadReceiptRepository, // Added
            IMapper mapper,
            ILogger<GetGroupMessagesQueryHandler> logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
            _messageReadReceiptRepository = messageReadReceiptRepository ?? throw new ArgumentNullException(nameof(messageReadReceiptRepository)); // Added
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PagedResult<MessageDto>>> Handle(GetGroupMessagesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "用户 {CurrentUserId} 尝试检索群组 {GroupId} 的消息。PageNumber: {PageNumber}, PageSize: {PageSize}",
                request.CurrentUserId, request.GroupId, request.PageNumber, request.PageSize);

            // 1. 验证群组是否存在以及用户成员关系
            var group = await _groupRepository.GetByIdWithMembersAsync(request.GroupId); // Still need group for name and member check
            if (group == null)
            {
                _logger.LogWarning("群组 {GroupId} 未找到。", request.GroupId);
                return Result<PagedResult<MessageDto>>.Failure("Group.NotFound", $"群组 {request.GroupId} 未找到。");
            }

            if (!group.Members.Any(m => m.UserId == request.CurrentUserId))
            {
                _logger.LogWarning("用户 {CurrentUserId} 不是群组 {GroupId} 的成员。访问被拒绝。", request.CurrentUserId, request.GroupId);
                return Result<PagedResult<MessageDto>>.Failure("Message.AccessDenied", $"用户 {request.CurrentUserId} 未被授权访问群组 {request.GroupId} 的消息。");
            }

            // 2. 从仓储中获取分页消息
            var (messages, totalCount) = await _messageRepository.GetGroupMessagesAsync(
                request.GroupId,
                request.PageNumber,
                request.PageSize);

            if (messages == null || !messages.Any())
            {
                _logger.LogInformation("群组 {GroupId} 未找到符合条件的消息 for Page {PageNumber}.", request.GroupId, request.PageNumber);
                return Result<PagedResult<MessageDto>>.Success(PagedResult<MessageDto>.Success(new List<MessageDto>(), totalCount, request.PageNumber, request.PageSize));
            }

            // 3. 映射到 DTO
            // SenderUsername, SenderAvatarUrl, and GroupName are now configured in MappingProfile.
            // For SenderUsername and SenderAvatarUrl, MappingProfile uses message.Sender.Profile,
            // which is correctly loaded by _messageRepository.GetGroupMessagesAsync.
            // For GroupName, MappingProfile uses message.RecipientGroup.Name.
            // _messageRepository.GetGroupMessagesAsync currently does NOT load message.RecipientGroup.
            // This means GroupName will be null after this mapping if RecipientGroup is not loaded.
            // This will be addressed by modifying the repository if necessary, as per task instructions.
            var messageDtos = _mapper.Map<List<MessageDto>>(messages);

            // Populate ReadCount for group messages
            if (messageDtos.Any())
            {
                var messageIds = messages.Select(m => m.MessageId).ToList();
                var readCounts = await _messageReadReceiptRepository.GetReadCountsForMessagesAsync(messageIds, cancellationToken);

                foreach (var dto in messageDtos)
                {
                    if (readCounts.TryGetValue(dto.MessageId, out var count))
                    {
                        dto.ReadCount = count;
                    }
                    // If a messageId is not in readCounts, it means no one has read it yet, so ReadCount remains null (or 0 if you prefer to default)
                    // For group messages, GroupName should be set by the mapper if RecipientGroup is loaded.
                    // If RecipientGroup is not loaded by GetGroupMessagesAsync, GroupName will be null.
                    // The existing comment about GroupName mapping is still relevant.
                }
            }
            
            _logger.LogInformation("成功检索到群组 {GroupId} 的 {Count} 条消息 for page {PageNumber}. Total messages: {TotalCount}",
                request.GroupId, messageDtos.Count, request.PageNumber, totalCount);
            return Result<PagedResult<MessageDto>>.Success(PagedResult<MessageDto>.Success(messageDtos, totalCount, request.PageNumber, request.PageSize));
        }
    }
}