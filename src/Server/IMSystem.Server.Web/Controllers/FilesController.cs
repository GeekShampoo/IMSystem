using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.Files;
using IMSystem.Protocol.DTOs.Responses.Files;
using IMSystem.Server.Core.Features.Files.Commands;
using IMSystem.Server.Core.Features.Files.Queries;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Web.Common;
using IMSystem.Protocol.Enums; // Added for ApiErrorCode
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IMSystem.Server.Web.Controllers;

/// <summary>
/// 处理文件上传和管理相关操作的API控制器。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // 大部分文件操作需要授权
public class FilesController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<FilesController> _logger;
    private readonly IFileStorageService _fileStorageService;

    public FilesController(
        IMediator mediator,
        IMapper mapper,
        ILogger<FilesController> logger,
        IFileStorageService fileStorageService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
    }

    /// <summary>
    /// 请求文件上传并获取预签名URL。
    /// </summary>
    /// <param name="request">请求上传文件的参数。</param>
    /// <returns>包含预签名URL和文件元数据ID的响应。</returns>
    [HttpPost("request-upload")]
    [ProducesResponseType(typeof(RequestFileUploadResponse), StatusCodes.Status200OK)]
    // 400, 401, 500等错误由DoAction约定和BaseApiController.HandleResult覆盖
    public async Task<IActionResult> RequestFileUpload([FromBody] RequestFileUploadRequest request)
    {
        var command = new RequestFileUploadCommand(
            request.FileName,
            request.ContentType,
            request.FileSize,
            CurrentUserId
        );

        var result = await _mediator.Send(command);
        
        return HandleResult(result, value => Ok(value));
    }

    /// <summary>
    /// 确认文件上传已完成。
    /// </summary>
    /// <param name="request">确认文件上传的参数。</param>
    /// <returns>更新后的文件元数据。</returns>
    [HttpPost("confirm-upload")]
    [ProducesResponseType(typeof(FileMetadataDto), StatusCodes.Status200OK)]
    // 400, 401, 404, 500等错误由DoAction约定和BaseApiController.HandleResult覆盖
    public async Task<IActionResult> ConfirmFileUpload([FromBody] ConfirmFileUploadRequest request)
    {
        var command = new ConfirmFileUploadCommand(
            request.FileMetadataId,
            CurrentUserId
        );

        var result = await _mediator.Send(command);
        
        return HandleResult(result, value => Ok(value));
    }

    /// <summary>
    /// 通过令牌接收并保存上传的文件（用于LocalFileStorageService的"预签名"URL）。
    /// </summary>
    /// <param name="targetFile">目标存储文件名 (来自查询参数)。</param>
    /// <param name="contentType">文件内容类型 (来自查询参数)。</param>
    /// <param name="token">验证令牌 (来自查询参数)。</param>
    /// <param name="expires">令牌过期时间 (来自查询参数, ISO 8601格式)。</param>
    /// <param name="size">文件大小 (可选, 来自查询参数)。</param>
    /// <returns>操作结果。</returns>
    [HttpPut("upload-by-token")]
    [AllowAnonymous] // 此端点通过令牌进行身份验证，而不是JWT Bearer token
    [ProducesResponseType(StatusCodes.Status200OK)] // 只保留一个成功状态码
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)] // 对应Token无效或过期
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)] // 对应DirectoryError, StorageError
    public async Task<IActionResult> UploadViaToken(
        [FromQuery] string targetFile,
        [FromQuery] string contentType,
        [FromQuery] string token,
        [FromQuery] string expires,
        [FromQuery] long? size,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UploadViaToken request received: TargetFile={TargetFile}, ContentType={ContentType}, Token={Token}, Expires={Expires}, Size={Size}",
            targetFile, contentType, token, expires, size);

        var result = await _fileStorageService.SaveUploadedFileViaTokenAsync(
            targetFile,
            contentType,
            token,
            expires,
            Request.Body,
            size,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("UploadViaToken failed for TargetFile={TargetFile}: {ErrorCode} - {ErrorMessage}",
                targetFile, result.Error.Code, result.Error.Message);

            ApiErrorResponse errorResponse;
            int statusCode;

            if (result.Error.Code.Contains("InvalidParams", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCodes.Status400BadRequest;
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.ValidationFailed, detail: result.Error.Message, instance: HttpContext.Request.Path, message: result.Error.Message);
            }
            else if (result.Error.Code.Contains("Expired", StringComparison.OrdinalIgnoreCase) ||
                     result.Error.Code.Contains("InvalidToken", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCodes.Status401Unauthorized;
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.AuthenticationFailed, detail: result.Error.Message, instance: HttpContext.Request.Path, message: result.Error.Message);
            }
            else if (result.Error.Code.Contains("InvalidPath", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCodes.Status400BadRequest;
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.ValidationFailed, detail: result.Error.Message, instance: HttpContext.Request.Path, message: "Invalid file name or path.");
            }
            else if (result.Error.Code.Contains("DirectoryError", StringComparison.OrdinalIgnoreCase) ||
                     result.Error.Code.Contains("StorageError", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCodes.Status500InternalServerError;
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.ServerError, detail: result.Error.Message, instance: HttpContext.Request.Path, message: "Could not create storage directory or save file.");
            }
            else
            {
                statusCode = StatusCodes.Status400BadRequest; // Default to BadRequest for other errors from the service
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.UnknownError, detail: result.Error.Message, instance: HttpContext.Request.Path, message: result.Error.Message);
            }
            return StatusCode(statusCode, errorResponse);
        }

        _logger.LogInformation("File {TargetFile} successfully uploaded via token.", targetFile);
        return Ok($"File {targetFile} uploaded successfully.");
    }

    /// <summary>
    /// 通过令牌下载文件（用于LocalFileStorageService生成的"预签名"下载URL）。
    /// </summary>
    /// <param name="targetFile">目标存储文件名 (来自查询参数)。</param>
    /// <param name="userId">请求下载的用户ID (来自查询参数, 用于验证)。</param>
    /// <param name="token">验证令牌 (来自查询参数)。</param>
    /// <param name="expires">令牌过期时间 (来自查询参数, ISO 8601格式)。</param>
    /// <returns>文件流或错误响应。</returns>
    [HttpGet("download-by-token")]
    [AllowAnonymous] // 通过令牌验证
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)] // 文件流
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadByToken(
        [FromQuery] string targetFile,
        [FromQuery] Guid userId,
        [FromQuery] string token,
        [FromQuery] string expires,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DownloadByToken request received: TargetFile={TargetFile}, UserId={UserId}, Token={Token}, Expires={Expires}",
            targetFile, userId, token, expires);

        var result = await _fileStorageService.GetFileStreamViaTokenAsync(
            targetFile,
            userId,
            token,
            expires,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("DownloadByToken failed for TargetFile={TargetFile}: {ErrorCode} - {ErrorMessage}",
                targetFile, result.Error.Code, result.Error.Message);

            ApiErrorResponse errorResponse;
            int statusCode;

            if (result.Error.Code.Contains("InvalidParams", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCodes.Status400BadRequest;
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.ValidationFailed, detail: result.Error.Message, instance: HttpContext.Request.Path, message: result.Error.Message);
            }
            else if (result.Error.Code.Contains("Expired", StringComparison.OrdinalIgnoreCase) ||
                     result.Error.Code.Contains("InvalidToken", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCodes.Status401Unauthorized;
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.AuthenticationFailed, detail: result.Error.Message, instance: HttpContext.Request.Path, message: result.Error.Message);
            }
            else if (result.Error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase)) // Corresponds to "FileNotFound"
            {
                statusCode = StatusCodes.Status404NotFound;
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.ResourceNotFound, detail: result.Error.Message, instance: HttpContext.Request.Path, message: result.Error.Message);
            }
            else if (result.Error.Code.Contains("InvalidPath", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCodes.Status400BadRequest;
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.ValidationFailed, detail: result.Error.Message, instance: HttpContext.Request.Path, message: "Invalid file name or path specified for download.");
            }
            else if (result.Error.Code.Contains("StorageError", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCodes.Status500InternalServerError; // Storage errors are typically server-side
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.ServerError, detail: result.Error.Message, instance: HttpContext.Request.Path, message: "A storage error occurred while trying to access the file.");
            }
            else // Default for other service errors
            {
                statusCode = StatusCodes.Status400BadRequest;
                errorResponse = ApiErrorFactory.Create(ApiErrorCode.UnknownError, detail: result.Error.Message, instance: HttpContext.Request.Path, message: result.Error.Message);
            }
            return StatusCode(statusCode, errorResponse);
        }

        var fileDownloadInfo = result.Value;
        _logger.LogInformation("Providing file {OriginalFileName} (Stored: {TargetFile}) for download via token.",
            fileDownloadInfo.OriginalFileName, targetFile);

        // 对于某些浏览器，请确保文件名已为 Content-Disposition 标头正确编码。
        var contentDisposition = new System.Net.Mime.ContentDisposition
        {
            FileName = fileDownloadInfo.OriginalFileName,
            Inline = false // false = 强制下载, true = 如果支持则尝试在浏览器中显示
        };
        Response.Headers.Append("Content-Disposition", contentDisposition.ToString());
        
        // FileStreamResult 将负责释放流。
        return File(fileDownloadInfo.ContentStream, fileDownloadInfo.ContentType);
    }

    /// <summary>
    /// 根据文件元数据ID下载文件。
    /// 此端点需要授权，并可能包含额外的权限检查逻辑。
    /// </summary>
    /// <param name="fileMetadataId">要下载的文件的元数据ID。</param>
    /// <returns>文件流或错误响应。</returns>
    [HttpGet("download/{fileMetadataId:guid}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)] // 文件流
    // 401, 403, 404, 500等错误由GetById约定和BaseApiController.HandleFailure覆盖
    public async Task<IActionResult> Download(Guid fileMetadataId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("接收到 Download 请求: FileMetadataId={FileMetadataId}", fileMetadataId);

        var query = new GetFileMetadataByIdQuery(fileMetadataId, CurrentUserId);
        var queryResult = await _mediator.Send(query, cancellationToken);

        if (!queryResult.IsSuccess)
        {
            _logger.LogWarning("获取文件元数据失败: Code={ErrorCode}, Message={ErrorMessage}, FileMetadataId: {FileMetadataId}",
                queryResult.Error.Code, queryResult.Error.Message, fileMetadataId);

            return HandleFailure(queryResult);
        }

        var fileMetadataDto = queryResult.Value;

        // 确保 FileMetadataDto 包含必要信息
        if (string.IsNullOrWhiteSpace(fileMetadataDto.StoredFileName) ||
            string.IsNullOrWhiteSpace(fileMetadataDto.FileName) ||
            string.IsNullOrWhiteSpace(fileMetadataDto.ContentType))
        {
            _logger.LogError("从查询获取的 FileMetadataDto 缺少必要的 StoredFileName, FileName, 或 ContentType。FileMetadataId: {FileMetadataId}", fileMetadataId);
            return StatusCode(StatusCodes.Status500InternalServerError, Result.Failure("File.Metadata.Incomplete", "文件元数据不完整，无法下载.").Error.Message);
        }

        // 通过服务获取文件流
        var fileStreamResult = await _fileStorageService.GetFileStreamAsync(
            fileMetadataDto.StoredFileName,
            fileMetadataDto.FileName,
            fileMetadataDto.ContentType,
            cancellationToken);

        if (!fileStreamResult.IsSuccess)
        {
            _logger.LogWarning("通过 FileStorageService 获取文件流失败: Code={ErrorCode}, Message={ErrorMessage}, StoredFileName: {StoredFileName}",
                fileStreamResult.Error.Code, fileStreamResult.Error.Message, fileMetadataDto.StoredFileName);

            return HandleFailure(fileStreamResult);
        }

        var fileDownloadInfo = fileStreamResult.Value;

        // 返回文件流
        var contentDisposition = new System.Net.Mime.ContentDisposition
        {
            FileName = fileDownloadInfo.OriginalFileName,
            Inline = false // false = 强制下载
        };
        Response.Headers.Append("Content-Disposition", contentDisposition.ToString());
        
        _logger.LogInformation("正在提供文件 {OriginalFileName} (Stored: {StoredFileName}, 元数据ID: {FileMetadataId}) 下载。",
            fileDownloadInfo.OriginalFileName, fileMetadataDto.StoredFileName, fileMetadataId);
        
        // FileStreamResult 将负责释放 fileDownloadInfo.ContentStream
        return File(fileDownloadInfo.ContentStream, fileDownloadInfo.ContentType);
    }

    /// <summary>
    /// 删除指定的文件。
    /// </summary>
    /// <param name="fileId">要删除的文件的元数据ID。</param>
    /// <returns>操作结果。</returns>
    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)] // 标准的成功删除响应
    // 400, 401, 403, 404, 500等错误由Delete约定和BaseApiController.HandleResult覆盖
    public async Task<IActionResult> DeleteFile(Guid fileId)
    {
        _logger.LogInformation("接收到 DeleteFile 请求: FileId={FileId}", fileId);

        var command = new DeleteFileCommand(fileId, CurrentUserId);
        var result = await _mediator.Send(command);

        return HandleResult(result, () => NoContent());
    }
}