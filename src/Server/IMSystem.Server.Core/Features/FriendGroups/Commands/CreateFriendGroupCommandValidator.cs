using FluentValidation;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

public class CreateFriendGroupCommandValidator : AbstractValidator<CreateFriendGroupCommand>
{
    public CreateFriendGroupCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("用户ID不能为空。");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("分组名称不能为空。")
            .MaximumLength(50).WithMessage("分组名称长度不能超过50个字符。");
        
        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("排序序号必须大于或等于0。");
    }
}