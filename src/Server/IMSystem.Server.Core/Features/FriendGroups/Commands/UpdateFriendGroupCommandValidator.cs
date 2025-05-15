using FluentValidation;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

public class UpdateFriendGroupCommandValidator : AbstractValidator<UpdateFriendGroupCommand>
{
    public UpdateFriendGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("分组ID不能为空。");

        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("当前用户ID不能为空。");

        // 至少提供一个要更新的字段
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.NewName) || x.NewOrder.HasValue)
            .WithMessage("至少需要提供新的分组名称或排序序号中的一个进行更新。");

        When(x => !string.IsNullOrWhiteSpace(x.NewName), () =>
        {
            RuleFor(x => x.NewName)
                .MaximumLength(50).WithMessage("分组名称长度不能超过50个字符。")
                .MinimumLength(1).WithMessage("分组名称长度至少为1个字符。");
        });

        When(x => x.NewOrder.HasValue, () =>
        {
            RuleFor(x => x.NewOrder)
                .GreaterThanOrEqualTo(0).WithMessage("排序序号必须大于或等于0。");
        });
    }
}