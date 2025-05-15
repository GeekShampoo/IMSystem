using FluentValidation;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

public class AddFriendToGroupCommandValidator : AbstractValidator<AddFriendToGroupCommand>
{
    public AddFriendToGroupCommandValidator()
    {
        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("当前用户ID不能为空。");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("分组ID不能为空。");

        RuleFor(x => x.FriendshipId)
            .NotEmpty().WithMessage("好友关系ID不能为空。");
    }
}