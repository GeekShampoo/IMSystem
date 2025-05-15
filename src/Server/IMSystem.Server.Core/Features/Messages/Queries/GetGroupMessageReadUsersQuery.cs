using System;
using MediatR;
using IMSystem.Protocol.DTOs.Responses.Messages;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Messages.Queries
{
    /// <summary>
    /// Query to get the list of users who have read a specific group message.
    /// </summary>
    public class GetGroupMessageReadUsersQuery : IRequest<Result<GetGroupMessageReadUsersResponse>>
    {
        /// <summary>
        /// The ID of the message.
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// The ID of the user requesting the information.
        /// </summary>
        public Guid UserId { get; set; }

        public GetGroupMessageReadUsersQuery(Guid messageId, Guid userId)
        {
            MessageId = messageId;
            UserId = userId;
        }
    }
}