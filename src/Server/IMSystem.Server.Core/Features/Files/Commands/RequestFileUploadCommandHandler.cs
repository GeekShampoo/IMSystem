using MediatR;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Files;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Events; // For FileUploadInitiatedEvent
using Microsoft.Extensions.Logging;
using System;
using System.IO; // For Path.GetExtension
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Events.Files;

namespace IMSystem.Server.Core.Features.Files.Commands;

/// <summary>
/// 处理 RequestFileUploadCommand 的处理器。
/// </summary>
public class RequestFileUploadCommandHandler : IRequestHandler<RequestFileUploadCommand, Result<RequestFileUploadResponse>>
{
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository; // 添加用户仓储以获取上传者实体
    private readonly ILogger<RequestFileUploadCommandHandler> _logger;

    public RequestFileUploadCommandHandler(
        IFileMetadataRepository fileMetadataRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository, // 添加用户仓储
        ILogger<RequestFileUploadCommandHandler> logger)
    {
        _fileMetadataRepository = fileMetadataRepository ?? throw new ArgumentNullException(nameof(fileMetadataRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<RequestFileUploadResponse>> Handle(RequestFileUploadCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始处理 RequestFileUploadCommand，文件名: {FileName}, 类型: {ContentType}, 大小: {FileSize}, 请求者ID: {RequesterId}",
            request.FileName, request.ContentType, request.FileSize, request.RequesterId);

        try
        {
            // 获取上传者用户实体，用于挂载领域事件
            var uploader = await _userRepository.GetByIdAsync(request.RequesterId, cancellationToken);
            if (uploader == null)
            {
                _logger.LogError("上传者用户 {RequesterId} 不存在", request.RequesterId);
                return Result<RequestFileUploadResponse>.Failure("User.NotFound", "上传者用户不存在。");
            }

            // 1. 生成存储文件名 (例如：用户ID/年/月/日/GUID.扩展名)
            //    确保文件名对于存储提供商是唯一的且安全的。
            var fileExtension = Path.GetExtension(request.FileName); // 获取原始文件扩展名
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            // 考虑更结构化的路径，例如按用户或日期
            // var storedFileName = $"{request.RequesterId}/{DateTime.UtcNow:yyyyMMdd}/{uniqueFileName}";
            var storedFileName = uniqueFileName; // 简化示例，实际项目中应考虑路径策略

            // 2. 创建 FileMetadata 实体
            var fileMetadata = new FileMetadata(
                fileName: request.FileName,
                storedFileName: storedFileName,
                contentType: request.ContentType,
                fileSize: request.FileSize,
                uploaderId: request.RequesterId,
                storageProvider: _fileStorageService.ProviderName // 从文件存储服务获取提供程序名称
            );
            // IsConfirmed 默认为 false

            // 3. 调用 IFileStorageService 生成预签名上传 URL
            //    预签名URL通常需要文件名、内容类型，有时还有内容长度限制。
            var preSignedUrlResult = await _fileStorageService.GetPresignedUploadUrlAsync(
                storedFileName,
                request.ContentType,
                request.FileSize, // 可选，某些提供商可能需要
                cancellationToken);

            if (preSignedUrlResult == null || string.IsNullOrEmpty(preSignedUrlResult.Url))
            {
                _logger.LogError("为文件 {StoredFileName} 生成预签名上传URL失败。", storedFileName);
                return Result<RequestFileUploadResponse>.Failure("File.PresignedUrlError", "无法生成文件上传URL，请稍后重试。");
            }

            // 4. 将 FileMetadata 实体添加到仓储
            await _fileMetadataRepository.AddAsync(fileMetadata, cancellationToken);

            // 5. 创建 FileUploadInitiatedEvent 领域事件并添加到上传者实体
            var initiatedEvent = new FileUploadInitiatedEvent(
                fileMetadataId: fileMetadata.Id,
                fileName: fileMetadata.FileName,
                storedFileName: fileMetadata.StoredFileName,
                contentType: fileMetadata.ContentType,
                fileSize: fileMetadata.FileSize,
                uploaderId: fileMetadata.CreatedBy ?? Guid.Empty, // CreatedBy 即为 RequesterId
                preSignedUploadUrl: preSignedUrlResult.Url // 将预签名URL包含在事件中
            );
            
            // 将事件添加到上传者实体，由 ApplicationDbContext 统一处理
            uploader.AddDomainEvent(initiatedEvent);

            // 6. 保存更改到数据库 (包括 FileMetadata 和领域事件)
            var success = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
            if (!success)
            {
                _logger.LogError("保存文件元数据 {FileMetadataId} 到数据库失败。", fileMetadata.Id);
                // 此处可能需要回滚或补偿操作，例如尝试删除已生成的预签名URL（如果可能）
                return Result<RequestFileUploadResponse>.Failure("File.StorageError", "保存文件信息失败。");
            }

            _logger.LogInformation("文件上传请求处理成功，FileMetadataId: {FileMetadataId}, PreSignedUrl: {PreSignedUrl}",
                fileMetadata.Id, preSignedUrlResult.Url);

            // 7. 返回响应
            return Result<RequestFileUploadResponse>.Success(new RequestFileUploadResponse
            {
                FileMetadataId = fileMetadata.Id,
                PreSignedUrl = preSignedUrlResult.Url,
                HttpMethod = preSignedUrlResult.HttpMethod ?? "PUT" // 从服务获取实际的HTTP方法
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 RequestFileUploadCommand 时发生错误，文件名: {FileName}", request.FileName);
            return Result<RequestFileUploadResponse>.Failure("File.UnexpectedError", $"处理文件上传请求时发生内部错误: {ex.Message}");
        }
    }
}