using FluentValidation;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.CreatorUserId)
            .NotEmpty().WithMessage("创建者用户ID不能为空。");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("群组名称不能为空。")
            .Length(1, 100).WithMessage("群组名称长度必须在1到100个字符之间。");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("群组描述不能超过500个字符。");

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(2048).WithMessage("头像URL过长。")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _) || string.IsNullOrEmpty(uri))
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl))
            .WithMessage("无效的头像URL格式。");
    }
}