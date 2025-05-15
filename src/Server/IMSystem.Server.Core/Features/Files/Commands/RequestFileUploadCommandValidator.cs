using FluentValidation;
using System;
using IMSystem.Server.Core.Settings;
using Microsoft.Extensions.Options;

namespace IMSystem.Server.Core.Features.Files.Commands
{
    /// <summary>
    /// RequestFileUploadCommand 的验证器。
    /// </summary>
    public class RequestFileUploadCommandValidator : AbstractValidator<RequestFileUploadCommand>
    {
        private readonly FileUploadSettings _settings;

        public RequestFileUploadCommandValidator(IOptions<FileUploadSettings> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _settings = options.Value;

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("文件名不能为空。")
                .MaximumLength(255).WithMessage("文件名长度不能超过255个字符。")
                .Must(BeAValidFileName).WithMessage("文件名包含无效字符。");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("文件内容类型不能为空。")
                .MaximumLength(100).WithMessage("内容类型长度不能超过100个字符。")
                .Must(BeAnAllowedContentType).WithMessage(x =>
                    $"不支持的文件类型: {x.ContentType}。允许的类型: {string.Join(", ", _settings.AllowedContentTypes)}");

            RuleFor(x => x.FileSize)
                .GreaterThan(0).WithMessage("文件大小必须大于0字节。")
                .LessThanOrEqualTo(_settings.MaxFileSize)
                .WithMessage($"文件大小不能超过 {_settings.MaxFileSize / (1024 * 1024)} MB。");

            RuleFor(x => x.RequesterId)
                .NotEqual(Guid.Empty).WithMessage("请求者ID无效。");
        }

        private bool BeAValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            return fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) == -1;
        }

        private bool BeAnAllowedContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return false;
            return _settings.AllowedContentTypes.Contains(contentType.ToLowerInvariant());
        }
    }
}