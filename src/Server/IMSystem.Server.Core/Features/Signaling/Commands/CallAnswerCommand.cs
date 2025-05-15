using System;
using IMSystem.Protocol.Enums;
using MediatR;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Signaling.Commands
{
    /// <summary>
    /// 通话应答命令
    /// </summary>
    public class CallAnswerCommand : IRequest<Result>
    {
        public Guid CallerId { get; set; }
        public Guid CalleeId { get; set; }
        public Guid CallId { get; set; }
        public bool Accepted { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public CallAnswerCommand(Guid callerId, Guid calleeId, Guid callId, bool accepted, DateTimeOffset timestamp)
        {
            CallerId = callerId;
            CalleeId = calleeId;
            CallId = callId;
            Accepted = accepted;
            Timestamp = timestamp;
        }
    }
}