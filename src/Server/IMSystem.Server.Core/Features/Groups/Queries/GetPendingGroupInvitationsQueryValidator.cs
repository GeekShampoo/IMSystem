using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Groups.Queries;

public class GetPendingGroupInvitationsQueryValidator : AbstractValidator<GetPendingGroupInvitationsQuery>
{
    public GetPendingGroupInvitationsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("用户ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("用户ID格式无效。");
    }
}