using MediatR;
using IMSystem.Server.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Events.Files;

namespace IMSystem.Server.Core.Features.Files.Events;

/// <summary>
/// 处理 FileUploadInitiatedEvent 的事件处理器。
/// </summary>
public class FileUploadInitiatedEventHandler : INotificationHandler<FileUploadInitiatedEvent>
{
    private readonly ILogger<FileUploadInitiatedEventHandler> _logger;

    public FileUploadInitiatedEventHandler(ILogger<FileUploadInitiatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(FileUploadInitiatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("文件上传已初始化：FileMetadataId: {FileMetadataId}, FileName: {FileName}, UploaderId: {UploaderId}, PreSignedUrl: {PreSignedUrl}",
            notification.FileMetadataId,
            notification.FileName,
            notification.UploaderId,
            notification.PreSignedUploadUrl);

        // 在这里可以添加其他逻辑，例如：
        // - 发送通知给相关系统或用户
        // - 记录更详细的审计信息

        return Task.CompletedTask;
    }
}