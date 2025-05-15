using System;
using IMSystem.Protocol.Enums;
using MediatR;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Signaling.Commands
{
    /// <summary>
    /// 发起通话邀请命令
    /// </summary>
    public class CallInviteCommand : IRequest<Result>
    {
        public Guid CallerId { get; set; }
        public Guid CalleeId { get; set; }
        public CallType CallType { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public CallInviteCommand(Guid callerId, Guid calleeId, CallType callType, DateTimeOffset timestamp)
        {
            CallerId = callerId;
            CalleeId = calleeId;
            CallType = callType;
            Timestamp = timestamp;
        }
    }
}