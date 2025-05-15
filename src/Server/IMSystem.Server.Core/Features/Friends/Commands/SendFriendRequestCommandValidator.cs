using FluentValidation;

namespace IMSystem.Server.Core.Features.Friends.Commands;

public class SendFriendRequestCommandValidator : AbstractValidator<SendFriendRequestCommand>
{
    public SendFriendRequestCommandValidator()
    {
        RuleFor(x => x.RequesterId)
            .NotEmpty().WithMessage("请求者用户ID不能为空。");

        RuleFor(x => x.AddresseeId)
            .NotEmpty().WithMessage("目标用户ID不能为空。");
        
        RuleFor(x => x)
            .Must(command => command.RequesterId != command.AddresseeId)
            .WithMessage("不能向自己发送好友请求。")
            .When(command => command.RequesterId != System.Guid.Empty && command.AddresseeId != System.Guid.Empty); // 仅当两个ID都有效时才比较
    }
}