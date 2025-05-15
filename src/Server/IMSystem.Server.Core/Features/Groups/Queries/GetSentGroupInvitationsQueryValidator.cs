using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Groups.Queries;

public class GetSentGroupInvitationsQueryValidator : AbstractValidator<GetSentGroupInvitationsQuery>
{
    public GetSentGroupInvitationsQueryValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("群组ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("群组ID格式无效。");

        RuleFor(x => x.RequestorUserId)
            .NotEmpty().WithMessage("请求用户ID不能为空。")
            .NotEqual(Guid.Empty).WithMessage("请求用户ID格式无效。");
    }
}