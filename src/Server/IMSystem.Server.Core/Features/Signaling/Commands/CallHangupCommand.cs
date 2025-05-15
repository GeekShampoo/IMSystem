using System;
using MediatR;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Signaling.Commands
{
    /// <summary>
    /// 通话挂断命令
    /// </summary>
    public class CallHangupCommand : IRequest<Result>
    {
        public Guid CallerId { get; set; }
        public Guid CalleeId { get; set; }
        public Guid CallId { get; set; }
        public string Reason { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public CallHangupCommand(Guid callerId, Guid calleeId, Guid callId, string reason, DateTimeOffset timestamp)
        {
            CallerId = callerId;
            CalleeId = calleeId;
            CallId = callId;
            Reason = reason;
            Timestamp = timestamp;
        }
    }
}