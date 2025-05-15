using AutoMapper;
using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.Common; // Added for PagedResult
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For Message entity
using IMSystem.Server.Domain.Enums; // Added for FriendshipStatus
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Messages.Queries
{
    public class GetUserMessagesQueryHandler : IRequestHandler<GetUserMessagesQuery, Result<PagedResult<MessageDto>>>
    {
        private readonly IMessageRepository _messageRepository;
        // private readonly IUserRepository _userRepository; // No longer needed here if MessageRepository includes sender details
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IMessageReadReceiptRepository _messageReadReceiptRepository; // Added
        private readonly IMapper _mapper;
        private readonly ILogger<GetUserMessagesQueryHandler> _logger;

        public GetUserMessagesQueryHandler(
            IMessageRepository messageRepository,
            IFriendshipRepository friendshipRepository,
            IMessageReadReceiptRepository messageReadReceiptRepository, // Added
            IMapper mapper,
            ILogger<GetUserMessagesQueryHandler> logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _friendshipRepository = friendshipRepository ?? throw new ArgumentNullException(nameof(friendshipRepository));
            _messageReadReceiptRepository = messageReadReceiptRepository ?? throw new ArgumentNullException(nameof(messageReadReceiptRepository)); // Added
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PagedResult<MessageDto>>> Handle(GetUserMessagesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Attempting to retrieve messages between User {CurrentUserId} and User {OtherUserId}. PageNumber: {PageNumber}, PageSize: {PageSize}",
                request.CurrentUserId, request.OtherUserId, request.PageNumber, request.PageSize);

            // Check friendship status before fetching messages
            var friendship = await _friendshipRepository.GetFriendshipBetweenUsersAsync(request.CurrentUserId, request.OtherUserId);

            if (friendship == null)
            {
                _logger.LogInformation("No friendship record found between User {CurrentUserId} and User {OtherUserId}. Returning empty message list.",
                    request.CurrentUserId, request.OtherUserId);
                return Result<PagedResult<MessageDto>>.Success(PagedResult<MessageDto>.Empty(request.PageNumber, request.PageSize));
            }

            if (friendship.Status == FriendshipStatus.Blocked)
            {
                _logger.LogInformation("Friendship between User {CurrentUserId} and User {OtherUserId} is blocked. Returning empty message list.",
                    request.CurrentUserId, request.OtherUserId);
                // It's a valid state, but no messages should be shown.
                return Result<PagedResult<MessageDto>>.Success(PagedResult<MessageDto>.Empty(request.PageNumber, request.PageSize));
            }
            
            if (friendship.Status != FriendshipStatus.Accepted)
            {
                 _logger.LogInformation("Friendship between User {CurrentUserId} and User {OtherUserId} is not accepted (Status: {Status}). Returning empty message list.",
                    request.CurrentUserId, request.OtherUserId, friendship.Status);
                return Result<PagedResult<MessageDto>>.Success(PagedResult<MessageDto>.Empty(request.PageNumber, request.PageSize));
            }

            // Fetch paged messages from the repository
            var (messages, totalCount) = await _messageRepository.GetUserMessagesAsync(
                request.CurrentUserId,
                request.OtherUserId,
                request.PageNumber,
                request.PageSize);

            if (messages == null || !messages.Any())
            {
                _logger.LogInformation("在用户 {CurrentUserId} 和用户 {OtherUserId} 之间未找到符合条件的消息 for Page {PageNumber}.",
                    request.CurrentUserId, request.OtherUserId, request.PageNumber);
                return Result<PagedResult<MessageDto>>.Success(PagedResult<MessageDto>.Success(new List<MessageDto>(), totalCount, request.PageNumber, request.PageSize));
            }

            // SenderUsername and SenderAvatarUrl are now configured in MappingProfile.
            // MappingProfile uses message.Sender.Profile, which is correctly loaded by
            // _messageRepository.GetUserMessagesAsync.
            var messageDtos = _mapper.Map<List<MessageDto>>(messages);

            // Populate ReadCount for any group messages that might be part of this list
            // This is more for future-proofing or if GetUserMessagesAsync could return mixed types.
            // For typical P2P, RecipientType won't be Group.
            var groupMessageDtos = messageDtos
                .Where(dto => dto.RecipientType == Protocol.Enums.ProtocolMessageRecipientType.Group)
                .ToList();

            if (groupMessageDtos.Any())
            {
                var groupMessageIds = groupMessageDtos.Select(m => m.MessageId).ToList();
                var readCounts = await _messageReadReceiptRepository.GetReadCountsForMessagesAsync(groupMessageIds, cancellationToken);

                foreach (var dto in groupMessageDtos)
                {
                    if (readCounts.TryGetValue(dto.MessageId, out var count))
                    {
                        dto.ReadCount = count;
                    }
                }
            }
            
            _logger.LogInformation("成功检索到 {Count} 条消息 for page {PageNumber}. Total messages: {TotalCount}", messageDtos.Count, request.PageNumber, totalCount);
            return Result<PagedResult<MessageDto>>.Success(PagedResult<MessageDto>.Success(messageDtos, totalCount, request.PageNumber, request.PageSize));
        }
    }
}