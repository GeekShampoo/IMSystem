using AutoMapper;
using MediatR;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Files;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services; // 可能需要 IFileStorageService 获取 AccessUrl
using IMSystem.Server.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Files.Commands;

/// <summary>
/// 处理 ConfirmFileUploadCommand 的处理器。
/// </summary>
public class ConfirmFileUploadCommandHandler : IRequestHandler<ConfirmFileUploadCommand, Result<FileMetadataDto>>
{
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly IFileStorageService _fileStorageService; // 用于生成最终的 AccessUrl
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ConfirmFileUploadCommandHandler> _logger;

    public ConfirmFileUploadCommandHandler(
        IFileMetadataRepository fileMetadataRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ConfirmFileUploadCommandHandler> logger)
    {
        _fileMetadataRepository = fileMetadataRepository ?? throw new ArgumentNullException(nameof(fileMetadataRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<FileMetadataDto>> Handle(ConfirmFileUploadCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始处理 ConfirmFileUploadCommand，FileMetadataId: {FileMetadataId}, ConfirmerId: {ConfirmerId}",
            request.FileMetadataId, request.ConfirmerId);

        try
        {
            // Corrected: GetByIdAsync in IGenericRepository does not take CancellationToken directly in its signature.
            var fileMetadata = await _fileMetadataRepository.GetByIdAsync(request.FileMetadataId);

            if (fileMetadata == null)
            {
                _logger.LogWarning("确认文件上传失败：未找到 FileMetadataId 为 {FileMetadataId} 的记录。", request.FileMetadataId);
                return Result<FileMetadataDto>.Failure("File.NotFound", "未找到指定的文件记录。");
            }

            if (fileMetadata.IsConfirmed)
            {
                _logger.LogInformation("文件 {FileMetadataId} 已经确认过，无需重复操作。", request.FileMetadataId);
                // 文件已确认，直接返回成功和当前数据。
                // 如果需要在API层面附加特定消息，可以在控制器中处理。
                var alreadyConfirmedDto = _mapper.Map<FileMetadataDto>(fileMetadata);
                return Result<FileMetadataDto>.Success(alreadyConfirmedDto);
            }

            // 权限验证：通常只有上传者可以确认
            if (fileMetadata.CreatedBy != request.ConfirmerId)
            {
                _logger.LogWarning("用户 {ConfirmerId} 尝试确认不属于自己的文件 {FileMetadataId} (上传者: {UploaderId})。",
                    request.ConfirmerId, request.FileMetadataId, fileMetadata.CreatedBy);
                return Result<FileMetadataDto>.Failure("File.AccessDenied", "您没有权限确认此文件。");
            }

            // 获取最终的访问URL (可选，取决于存储服务和业务逻辑)
            // 某些存储服务在文件上传后才能确定最终的、可能是永久的访问URL
            // 或者，如果预签名URL用于下载，则可能不需要在此处生成新的访问URL
            string? finalAccessUrl = fileMetadata.AccessUrl; // 默认为实体中可能已有的
            
            // 获取/生成永久公共访问链接
            // This URL should be suitable for long-term public access, like an avatar URL.
            // The IFileStorageService implementation (e.g., LocalFileStorageService)
            // is responsible for constructing this based on its configuration (e.g., a public base URL).
            if (string.IsNullOrWhiteSpace(finalAccessUrl) && !string.IsNullOrWhiteSpace(fileMetadata.StoredFileName))
            {
                 finalAccessUrl = await _fileStorageService.GetPublicUrlAsync(fileMetadata.StoredFileName, cancellationToken);
            }
            // 注意：IFileStorageService 需要有 GetPublicUrlAsync 方法的定义

            fileMetadata.ConfirmUpload(request.ConfirmerId, finalAccessUrl, request.ClientMessageId); // Pass ClientMessageId
            // FileUploadConfirmedEvent 将在 FileMetadata.ConfirmUpload 内部通过 AddDomainEvent 添加

            // 更新操作由 EF Core 跟踪机制处理，调用 SaveChangesAsync 即可
            // _fileMetadataRepository.UpdateAsync(fileMetadata, cancellationToken); // 通常不需要显式调用 UpdateAsync

            var success = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
            if (!success)
            {
                _logger.LogError("保存文件元数据 {FileMetadataId} 的确认状态到数据库失败。", fileMetadata.Id);
                return Result<FileMetadataDto>.Failure("File.StorageError", "确认文件上传失败，无法保存更改。");
            }

            _logger.LogInformation("文件 {FileMetadataId} 已成功确认为已上传。", fileMetadata.Id);

            var confirmedDto = _mapper.Map<FileMetadataDto>(fileMetadata);
            // 确保 UploaderUsername 被正确映射 (如果 FileMetadataDto 需要)
            // 这可能需要在 AutoMapper Profile 中配置，或者在这里手动填充
            // if (confirmedDto != null && string.IsNullOrEmpty(confirmedDto.UploaderUsername))
            // {
            //     var uploader = await _userRepository.GetByIdAsync(fileMetadata.CreatedBy ?? Guid.Empty, cancellationToken);
            //     if (uploader != null) confirmedDto.UploaderUsername = uploader.Username;
            // }


            return Result<FileMetadataDto>.Success(confirmedDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 ConfirmFileUploadCommand 时发生错误，FileMetadataId: {FileMetadataId}", request.FileMetadataId);
            return Result<FileMetadataDto>.Failure("File.UnexpectedError", $"确认文件上传时发生内部错误: {ex.Message}");
        }
    }
}