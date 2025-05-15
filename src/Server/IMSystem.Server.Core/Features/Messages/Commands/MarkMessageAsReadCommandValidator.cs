using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Messages.Commands;

/// <summary>
/// MarkMessageAsReadCommand 的验证器。
/// </summary>
public class MarkMessageAsReadCommandValidator : AbstractValidator<MarkMessageAsReadCommand>
{
    public MarkMessageAsReadCommandValidator()
    {
        RuleFor(x => x.UpToMessageId)
            .NotEmpty().WithMessage("消息ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("消息ID格式无效。");

        RuleFor(x => x.ReaderUserId)
            .NotEmpty().WithMessage("读取者用户ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("读取者用户ID格式无效。");
    }
}