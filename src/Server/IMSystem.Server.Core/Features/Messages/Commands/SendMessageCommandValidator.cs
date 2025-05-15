using FluentValidation;
using System;
using IMSystem.Server.Domain.Enums; // Added for MessageType

namespace IMSystem.Server.Core.Features.Messages.Commands
{
    public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageCommandValidator()
        {
            RuleFor(x => x.SenderId)
                .NotEmpty().WithMessage("发送者ID不能为空。");

            RuleFor(x => x.RecipientId)
                .NotEmpty().WithMessage("接收者ID不能为空。");
            
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("消息内容不能为空。")
                .MaximumLength(4000).WithMessage("消息内容不能超过4000个字符。");

            RuleFor(x => x.MessageType)
                .NotEmpty().WithMessage("消息类型不能为空。")
                .Must(BeAValidMessageType).WithMessage("无效的消息类型。");
            
            When(x => x.ClientMessageId.HasValue, () =>
            {
                RuleFor(x => x.ClientMessageId)
                    .NotEqual(Guid.Empty).WithMessage("客户端消息ID格式不正确。");
            });

            When(x => x.ReplyToMessageId.HasValue, () =>
            {
                RuleFor(x => x.ReplyToMessageId)
                    .NotEqual(Guid.Empty).WithMessage("回复消息ID格式不正确。");
            });
        }

        private bool BeAValidMessageType(string messageType)
        {
            if (string.IsNullOrWhiteSpace(messageType)) return false;
            // 确保 MessageType 字符串可以被解析为 Domain.Enums.MessageType 枚举
            // 这依赖于 IMSystem.Server.Domain.Enums.MessageType 的定义
            return Enum.TryParse<MessageType>(messageType, true, out _);
        }
    }
}