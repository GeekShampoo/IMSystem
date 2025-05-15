using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Messages;
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Messages.Queries
{
    public class GetGroupMessageReadUsersQueryHandler : IRequestHandler<GetGroupMessageReadUsersQuery, Result<GetGroupMessageReadUsersResponse>>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IMessageReadReceiptRepository _messageReadReceiptRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetGroupMessageReadUsersQueryHandler> _logger;

        public GetGroupMessageReadUsersQueryHandler(
            IMessageRepository messageRepository,
            IMessageReadReceiptRepository messageReadReceiptRepository,
            IGroupMemberRepository groupMemberRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<GetGroupMessageReadUsersQueryHandler> logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _messageReadReceiptRepository = messageReadReceiptRepository ?? throw new ArgumentNullException(nameof(messageReadReceiptRepository));
            _groupMemberRepository = groupMemberRepository ?? throw new ArgumentNullException(nameof(groupMemberRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<GetGroupMessageReadUsersResponse>> Handle(GetGroupMessageReadUsersQuery request, CancellationToken cancellationToken)
        {
            var message = await _messageRepository.GetByIdAsync(request.MessageId);
            if (message == null)
            {
                _logger.LogWarning("Message with ID {MessageId} not found.", request.MessageId);
                return Result<GetGroupMessageReadUsersResponse>.Failure(new Error("MessageNotFound", "Message not found."));
            }

            if (message.RecipientType != MessageRecipientType.Group)
            {
                _logger.LogWarning("Message with ID {MessageId} is not a group message. RecipientType: {RecipientType}", request.MessageId, message.RecipientType);
                return Result<GetGroupMessageReadUsersResponse>.Failure(new Error("NotAGroupMessage", "This operation is only valid for group messages."));
            }

            var isMember = await _groupMemberRepository.IsUserMemberOfGroupAsync(message.RecipientId, request.UserId, cancellationToken);
            if (!isMember)
            {
                _logger.LogWarning("User {UserId} is not a member of group {GroupId} for message {MessageId}.", request.UserId, message.RecipientId, request.MessageId);
                return Result<GetGroupMessageReadUsersResponse>.Failure(new Error("UserNotGroupMember", "User is not a member of the group."));
            }

            var readReceipts = await _messageReadReceiptRepository.GetByMessageIdAsync(request.MessageId, cancellationToken);
            var userIds = readReceipts.Select(rr => rr.ReaderUserId).Distinct().ToList();

            if (!userIds.Any())
            {
                return Result<GetGroupMessageReadUsersResponse>.Success(new GetGroupMessageReadUsersResponse
                {
                    MessageId = request.MessageId,
                    ReadUsers = Enumerable.Empty<UserSummaryDto>()
                });
            }

            var users = await _userRepository.GetUsersByIdsAsync(userIds, cancellationToken);
            var userSummaries = _mapper.Map<IEnumerable<UserSummaryDto>>(users);

            return Result<GetGroupMessageReadUsersResponse>.Success(new GetGroupMessageReadUsersResponse
            {
                MessageId = request.MessageId,
                ReadUsers = userSummaries
            });
        }
    }
}