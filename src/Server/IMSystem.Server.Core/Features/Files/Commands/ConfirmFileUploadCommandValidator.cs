using FluentValidation;
using System;

namespace IMSystem.Server.Core.Features.Files.Commands;

/// <summary>
/// ConfirmFileUploadCommand 的验证器。
/// </summary>
public class ConfirmFileUploadCommandValidator : AbstractValidator<ConfirmFileUploadCommand>
{
    public ConfirmFileUploadCommandValidator()
    {
        RuleFor(x => x.FileMetadataId)
            .NotEqual(Guid.Empty).WithMessage("文件元数据ID不能为空。");

        RuleFor(x => x.ConfirmerId)
            .NotEqual(Guid.Empty).WithMessage("确认者ID不能为空。");
    }
}