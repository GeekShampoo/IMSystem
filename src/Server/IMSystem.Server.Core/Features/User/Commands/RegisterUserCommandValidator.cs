using FluentValidation;

namespace IMSystem.Server.Core.Features.User.Commands;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名是必填项。")
            .Length(3, 50).WithMessage("用户名长度必须在 3 到 50 个字符之间。");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱是必填项。")
            .EmailAddress().WithMessage("无效的邮箱地址。")
            .MaximumLength(100).WithMessage("邮箱长度不能超过 100 个字符。");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码是必填项。")
            .MinimumLength(8).WithMessage("密码长度至少为 8 个字符。")
            .MaximumLength(100).WithMessage("密码长度不能超过 100 个字符。")
            .Matches("[A-Z]").WithMessage("密码必须包含至少一个大写字母。")
            .Matches("[a-z]").WithMessage("密码必须包含至少一个小写字母。")
            .Matches("[0-9]").WithMessage("密码必须包含至少一个数字。")
            .Matches("[^a-zA-Z0-9]").WithMessage("密码必须包含至少一个特殊字符。");
    }
}