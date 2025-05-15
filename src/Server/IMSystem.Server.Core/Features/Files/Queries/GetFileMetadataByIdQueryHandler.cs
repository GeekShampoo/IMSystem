using AutoMapper;
using MediatR;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Files;
using IMSystem.Server.Core.Interfaces.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Files.Queries;

/// <summary>
/// 处理 GetFileMetadataByIdQuery 的处理器。
/// </summary>
public class GetFileMetadataByIdQueryHandler : IRequestHandler<GetFileMetadataByIdQuery, Result<FileMetadataDto>>
{
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetFileMetadataByIdQueryHandler> _logger;

    public GetFileMetadataByIdQueryHandler(
        IFileMetadataRepository fileMetadataRepository,
        IMapper mapper,
        ILogger<GetFileMetadataByIdQueryHandler> logger)
    {
        _fileMetadataRepository = fileMetadataRepository ?? throw new ArgumentNullException(nameof(fileMetadataRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<FileMetadataDto>> Handle(GetFileMetadataByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始处理 GetFileMetadataByIdQuery: FileMetadataId={FileMetadataId}, RequesterId={RequesterId}",
            request.FileMetadataId, request.RequesterId);

        try
        {
            // Corrected: GetByIdAsync in IGenericRepository does not take CancellationToken directly in its signature.
            // CancellationToken is typically handled by the DbContext methods called within the repository implementation.
            var fileMetadata = await _fileMetadataRepository.GetByIdAsync(request.FileMetadataId);

            if (fileMetadata == null)
            {
                _logger.LogWarning("未找到 FileMetadataId 为 {FileMetadataId} 的文件记录。", request.FileMetadataId);
                return Result<FileMetadataDto>.Failure("File.NotFound", "找不到指定的文件记录。");
            }

            // 权限检查：
            // 示例：只允许上传者访问自己的文件元数据。
            // 可以根据业务需求扩展此逻辑，例如允许特定角色的管理员访问，或文件是否标记为公开等。
            bool canAccess = fileMetadata.CreatedBy == request.RequesterId;

            // if (fileMetadata.IsPublic) // 假设 FileMetadata 有 IsPublic 属性
            // {
            //     canAccess = true;
            // }
            // else if (await _userRoleService.IsAdminAsync(request.RequesterId)) // 假设有用户角色服务
            // {
            //     canAccess = true;
            // }

            if (!canAccess)
            {
                _logger.LogWarning("用户 {RequesterId} 尝试访问不属于自己的文件元数据 {FileMetadataId} (上传者: {UploaderId})。",
                    request.RequesterId, request.FileMetadataId, fileMetadata.CreatedBy);
                return Result<FileMetadataDto>.Failure("File.AccessDenied", "您没有权限访问此文件记录。");
            }
            
            if (!fileMetadata.IsConfirmed && fileMetadata.CreatedBy != request.RequesterId)
            {
                // 如果文件未确认，并且请求者不是上传者，则不允许查看（即使有其他权限）
                // 上传者在确认前应该能看到自己上传的记录
                 _logger.LogWarning("用户 {RequesterId} 尝试访问尚未确认的文件元数据 {FileMetadataId}，且非上传者。",
                    request.RequesterId, request.FileMetadataId);
                return Result<FileMetadataDto>.Failure("File.NotConfirmed", "文件尚未确认，无法访问。");
            }


            var fileMetadataDto = _mapper.Map<FileMetadataDto>(fileMetadata);
            _logger.LogInformation("成功检索到 FileMetadataId 为 {FileMetadataId} 的文件记录。", request.FileMetadataId);
            return Result<FileMetadataDto>.Success(fileMetadataDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 GetFileMetadataByIdQuery 时发生错误: FileMetadataId={FileMetadataId}", request.FileMetadataId);
            return Result<FileMetadataDto>.Failure("File.UnexpectedError", $"检索文件记录时发生内部错误: {ex.Message}");
        }
    }
}