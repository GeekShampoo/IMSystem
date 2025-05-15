using FluentValidation;

namespace IMSystem.Server.Core.Features.Authentication.Commands
{
    /// <summary>
    /// 验证 <see cref="LoginCommand"/> 的验证器。
    /// </summary>
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        /// <summary>
        /// 初始化 <see cref="LoginCommandValidator"/> 类的新实例。
        /// </summary>
        public LoginCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("用户名不能为空。")
                .MinimumLength(3).WithMessage("用户名的长度至少为3个字符。")
                .MaximumLength(50).WithMessage("用户名的长度不能超过50个字符。");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("密码不能为空。")
                .MinimumLength(6).WithMessage("密码的长度至少为6个字符。")
                .MaximumLength(100).WithMessage("密码的长度不能超过100个字符。");
            // 在实际应用中，密码策略可能更复杂，例如要求包含数字、特殊字符等。
        }
    }
}