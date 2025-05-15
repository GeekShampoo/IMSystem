using FluentValidation;

namespace IMSystem.Server.Core.Features.User.Commands;

public class UpdateUserPresenceCommandValidator : AbstractValidator<UpdateUserPresenceCommand>
{
    public UpdateUserPresenceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("用户ID不能为空。");

        // 可选：对 CustomStatus 的长度进行校验
        // RuleFor(x => x.CustomStatus)
        //     .MaximumLength(100).WithMessage("自定义状态长度不能超过100个字符。")
        //     .When(x => !string.IsNullOrEmpty(x.CustomStatus));
    }
}