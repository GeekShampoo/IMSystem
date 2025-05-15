using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
// using IMSystem.Server.Domain.Events; // Uncomment if FileDeletedEvent is implemented

namespace IMSystem.Server.Core.Features.Files.Commands
{
    public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Result>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileMetadataRepository _fileMetadataRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteFileCommandHandler> _logger;
        // private readonly IMediator _mediator; // Uncomment if publishing FileDeletedEvent

        public DeleteFileCommandHandler(
            IFileStorageService fileStorageService,
            IFileMetadataRepository fileMetadataRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteFileCommandHandler> logger
            // IMediator mediator // Uncomment if publishing FileDeletedEvent
            )
        {
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            _fileMetadataRepository = fileMetadataRepository ?? throw new ArgumentNullException(nameof(fileMetadataRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator)); // Uncomment
        }

        public async Task<Result> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始处理 DeleteFileCommand，FileMetadataId: {FileMetadataId}, DeleterUserId: {DeleterUserId}",
                request.FileMetadataId, request.DeleterUserId);

            // Corrected: GetByIdAsync in IGenericRepository does not take CancellationToken directly in its signature.
            var fileMetadata = await _fileMetadataRepository.GetByIdAsync(request.FileMetadataId);

            if (fileMetadata == null)
            {
                _logger.LogWarning("删除文件失败：未找到 FileMetadataId 为 {FileMetadataId} 的记录。", request.FileMetadataId);
                return Result.Failure("File.NotFound", "未找到指定的文件记录。");
            }

            // 权限检查
            if (fileMetadata.CreatedBy != request.DeleterUserId)
            {
                _logger.LogWarning("用户 {DeleterUserId} 尝试删除文件 {FileMetadataId} (上传者: {UploaderId}) 失败：权限不足。",
                    request.DeleterUserId, request.FileMetadataId, fileMetadata.CreatedBy);
                return Result.Failure("File.AccessDenied", "用户无权删除此文件。");
            }

            if (string.IsNullOrWhiteSpace(fileMetadata.StoredFileName))
            {
                _logger.LogError("文件元数据 (ID: {FileMetadataId}) 的 StoredFileName 为空，无法定位物理文件进行删除。", request.FileMetadataId);
                // This indicates a data integrity issue.
                return Result.Failure("File.InvalidMetadata", "文件元数据信息不完整，无法删除物理文件。");
            }

            string storedFileNameToDelete = fileMetadata.StoredFileName; // Capture for use after metadata is deleted

            try
            {
                // 1. 从数据库删除元数据
                // Assuming IFileMetadataRepository has a synchronous Remove or an async RemoveAsync
                // If Remove is synchronous and part of EF Core, it marks the entity for deletion.
                // Corrected: IGenericRepository defines a synchronous Remove method.
                _fileMetadataRepository.Remove(fileMetadata);
                
                var dbSuccess = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

                if (!dbSuccess)
                {
                    _logger.LogError("从数据库删除文件元数据 {FileMetadataId} 失败。数据库未报告任何更改。", request.FileMetadataId);
                    return Result.Failure("File.DbError", "删除文件元数据失败。");
                }
                _logger.LogInformation("文件元数据 (ID: {FileMetadataId}) 已成功从数据库删除。", request.FileMetadataId);

                // 2. 如果数据库删除成功，则删除物理文件
                try
                {
                    await _fileStorageService.DeletePhysicalFileAsync(storedFileNameToDelete);
                    _logger.LogInformation("物理文件 {StoredFileName} (对应元数据ID: {FileMetadataId}) 已成功删除。",
                        storedFileNameToDelete, request.FileMetadataId);
                }
                catch (Exception physicalDeleteEx)
                {
                    // 物理文件删除失败，但元数据已删除！这是一个严重的不一致状态。
                    _logger.LogCritical(physicalDeleteEx,
                        "严重错误：文件元数据 (ID: {FileMetadataId}) 已从数据库删除，但物理文件 {StoredFileName} 删除失败。需要手动清理。文件路径: {FilePath}",
                        request.FileMetadataId, storedFileNameToDelete, storedFileNameToDelete /* For clarity, StoredFileName is the path relative to storage root */);
                    // 向客户端报告错误，但要谨慎措辞，因为元数据层面操作已完成。
                    // It's a server-side issue now.
                    return Result.Failure("File.PhysicalDeleteError", $"文件记录已成功删除，但物理文件清理时遇到问题。请联系管理员。");
                }

                // (可选) 触发 FileDeletedEvent
                // var deletedEvent = new FileDeletedEvent(request.FileMetadataId, storedFileNameToDelete, request.DeleterUserId, DateTime.UtcNow);
                // await _mediator.Publish(deletedEvent, cancellationToken); // Or add to Outbox

                return Result.Success();
            }
            catch (Exception ex) // Catches exceptions from DB operations primarily
            {
                _logger.LogError(ex, "处理 DeleteFileCommand 时发生意外错误，FileMetadataId: {FileMetadataId}", request.FileMetadataId);
                return Result.Failure("File.UnexpectedError", $"删除文件时发生内部错误: {ex.Message}");
            }
        }
    }
}