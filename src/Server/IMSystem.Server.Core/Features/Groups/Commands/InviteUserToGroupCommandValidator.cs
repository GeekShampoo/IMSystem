using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class InviteUserToGroupCommandValidator : AbstractValidator<InviteUserToGroupCommand>
{
    public InviteUserToGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("群组ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("群组ID格式无效。");

        RuleFor(x => x.InviterUserId) // Corrected to InviterUserId
            .NotEmpty().WithMessage("邀请者ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("邀请者ID格式无效。");

        RuleFor(x => x.InvitedUserId)
            .NotEmpty().WithMessage("被邀请用户ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("被邀请用户ID格式无效。")
            .NotEqual(x => x.InviterUserId).WithMessage("不能邀请自己加入群组。"); // Corrected to InviterUserId

        RuleFor(x => x.Message)
            .MaximumLength(500).WithMessage("邀请消息不能超过500个字符。")
            .When(x => !string.IsNullOrEmpty(x.Message)); // Only validate length if message is provided

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("邀请过期时间必须在当前时间之后。")
            .When(x => x.ExpiresAt.HasValue); // Only validate if ExpiresAt has a value
    }
}