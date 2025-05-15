using IMSystem.Protocol.Common; // Added for Result<T>
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Core.Settings;
using IMSystem.Server.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web; // For HttpUtility, consider System.Net.WebUtility.UrlEncode for newer projects

namespace IMSystem.Server.Infrastructure.FileStorage;

/// <summary>
/// 本地文件存储服务的实现。
/// 文件将存储在服务器本地文件系统的配置目录下。
/// "预签名URL" 将指向应用服务器的一个特定端点，用于接收文件上传。
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath; // 文件存储的根路径
    private readonly string _baseUrl;     // 用于构造可访问URL的基础URL (primarily for token-based downloads)
    private readonly string? _publicBaseUrl; // 用于构造公共静态文件URL
    private readonly IHttpContextAccessor _httpContextAccessor; // 用于获取当前请求的scheme和host
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _uploadTokenSecret; // 用于生成和验证上传令牌的密钥
    private readonly int _uploadTokenExpirationMinutes;
    private readonly int _downloadTokenExpirationMinutes;
    private readonly string _appBaseUrl; // 添加：应用基础URL
    private readonly string _uploadTokenEndpoint; // 添加：上传令牌端点路径
    private readonly string _downloadTokenEndpoint; // 添加：下载令牌端点路径

    public string ProviderName => "LocalFileSystem";

    public LocalFileStorageService(
        IOptions<LocalFileStorageOptions> options,
        IOptions<ApplicationSettings> appSettings,
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LocalFileStorageService> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var storageOptions = options?.Value ?? throw new ArgumentNullException(nameof(options), "LocalFileStorageOptions cannot be null.");
        var applicationSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings), "ApplicationSettings cannot be null.");

        _appBaseUrl = applicationSettings.BaseUrl;
        if (string.IsNullOrWhiteSpace(_appBaseUrl))
        {
            _logger.LogWarning("ApplicationSettings.BaseUrl is not configured. This is required for proper URL generation.");
        }

        _uploadTokenEndpoint = applicationSettings.ApiUrls.Files.UploadTokenEndpoint;
        _downloadTokenEndpoint = applicationSettings.ApiUrls.Files.DownloadTokenEndpoint;

        _publicBaseUrl = storageOptions.PublicBaseUrl; // Added

        _storagePath = !string.IsNullOrWhiteSpace(storageOptions.StoragePath)
            ? storageOptions.StoragePath
            : Path.Combine(env.ContentRootPath, "uploads");

        _baseUrl = storageOptions.BaseUrl;
        if (string.IsNullOrWhiteSpace(storageOptions.UploadTokenSecret))
        {
            _logger.LogError("LocalFileStorageOptions.UploadTokenSecret is not configured. This is required for secure operation.");
            throw new InvalidOperationException("UploadTokenSecret must be configured in LocalFileStorageOptions for production environments. Please provide a strong, static secret key.");
        }
        _uploadTokenSecret = storageOptions.UploadTokenSecret;

        _uploadTokenExpirationMinutes = storageOptions.UploadTokenExpirationMinutes > 0
            ? storageOptions.UploadTokenExpirationMinutes
            : 30;

        _downloadTokenExpirationMinutes = storageOptions.DownloadTokenExpirationMinutes > 0
            ? storageOptions.DownloadTokenExpirationMinutes
            : 60;

        if (string.IsNullOrEmpty(_baseUrl))
        {
            if (!string.IsNullOrEmpty(_appBaseUrl))
            {
                _baseUrl = $"{_appBaseUrl.TrimEnd('/')}{_downloadTokenEndpoint}";
                _logger.LogInformation("Setting BaseUrl from ApplicationSettings: {BaseUrl}", _baseUrl);
            }
            else
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                if (request != null)
                {
                    _baseUrl = $"{request.Scheme}://{request.Host}{_downloadTokenEndpoint}";
                }
                else
                {
                    _baseUrl = _downloadTokenEndpoint;
                    _logger.LogWarning("FileStorage:Local:BaseUrl 未配置，且无法从HttpContext获取，且ApplicationSettings.BaseUrl未配置。将使用默认值: {DefaultBaseUrl}", _baseUrl);
                }
            }
        }

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("本地文件存储路径 {StoragePath} 已创建。", _storagePath);
        }
        _logger.LogInformation("LocalFileStorageService 初始化完成，存储路径: {StoragePath}, 基础URL: {BaseUrl}, 公共基础URL: {PublicBaseUrl}", _storagePath, _baseUrl, _publicBaseUrl);
    }

    public Task<PreSignedUrlResult?> GetPresignedUploadUrlAsync(string storedFileName, string contentType, long? fileSize = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiration = DateTime.UtcNow.AddMinutes(_uploadTokenExpirationMinutes);
            var payload = $"{storedFileName}|{contentType}|{expiration:o}";
            var token = GenerateHmacToken(payload, _uploadTokenSecret);

            var baseUrl = GetApplicationBaseUrl();
            
            var uploadEndpointUrl = $"{baseUrl}{_uploadTokenEndpoint}";

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["targetFile"] = storedFileName;
            queryString["contentType"] = contentType;
            queryString["token"] = token;
            queryString["expires"] = expiration.ToString("o");
            if (fileSize.HasValue)
            {
                queryString["size"] = fileSize.Value.ToString();
            }

            var fullUploadUrl = $"{uploadEndpointUrl}?{queryString}";

            _logger.LogInformation("为文件 {StoredFileName} 生成的预签名上传URL: {UploadUrl}", storedFileName, fullUploadUrl);

            return Task.FromResult<PreSignedUrlResult?>(new PreSignedUrlResult
            {
                Url = fullUploadUrl,
                HttpMethod = "PUT"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为文件 {StoredFileName} 生成预签名上传URL时发生错误。", storedFileName);
            return Task.FromResult<PreSignedUrlResult?>(null);
        }
    }

    private string GenerateHmacToken(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }

    public bool ValidateHmacToken(string message, string token, string secret)
    {
        var expectedToken = GenerateHmacToken(message, secret);
        return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(token), Convert.FromBase64String(expectedToken));
    }

    public Task ConfirmUploadAsync(Guid fileMetadataId, string storedFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storagePath, storedFileName);
        if (File.Exists(filePath))
        {
            _logger.LogInformation("文件 {StoredFileName} (ID: {FileMetadataId}) 在本地存储中已确认存在。", storedFileName, fileMetadataId);
        }
        else
        {
            _logger.LogWarning("尝试确认文件 {StoredFileName} (ID: {FileMetadataId})，但文件在本地存储中未找到。", storedFileName, fileMetadataId);
            // 根据业务需求，如果文件不存在，这里可能需要抛出异常或执行其他操作。
            // 当前保持原逻辑，仅记录日志。
        }
        return Task.CompletedTask;
    }

    public Task<string?> GeneratePreSignedDownloadUrlAsync(Guid fileMetadataId, string storedFileName, Guid downloaderUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            _logger.LogError("GeneratePreSignedDownloadUrlAsync: storedFileName cannot be null or whitespace for FileMetadataId {FileMetadataId}.", fileMetadataId);
            throw new ArgumentException("Stored file name cannot be null or whitespace.", nameof(storedFileName));
        }

        _logger.LogInformation("GeneratePreSignedDownloadUrlAsync called for FileMetadataId: {FileMetadataId}, StoredFileName: {StoredFileName}, DownloaderUserId: {DownloaderUserId}",
            fileMetadataId, storedFileName, downloaderUserId);

        var expiration = DateTime.UtcNow.AddMinutes(_downloadTokenExpirationMinutes);
        var payload = $"{storedFileName}|{downloaderUserId}|{expiration:o}";
        var token = GenerateHmacToken(payload, _uploadTokenSecret);

        string baseUrl = GetApplicationBaseUrl();
        
        var finalDownloadUrlBase = $"{baseUrl}{_downloadTokenEndpoint}";

        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString["targetFile"] = storedFileName;
        queryString["userId"] = downloaderUserId.ToString();
        queryString["token"] = token;
        queryString["expires"] = expiration.ToString("o");
        
        var fullDownloadUrl = $"{finalDownloadUrlBase}?{queryString}";
        _logger.LogInformation("为文件 {StoredFileName} (ID: {FileMetadataId}) 生成的预签名下载URL: {DownloadUrl}", storedFileName, fileMetadataId, fullDownloadUrl);
        return Task.FromResult<string?>(fullDownloadUrl);
    }

    public async Task<FileMetadata> UploadFileAsync(Stream fileStream, string fileName, string contentType, Guid uploaderId, CancellationToken cancellationToken = default)
    {
        var fileExtension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(_storagePath, storedFileName);

        try
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using (var outputStream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(outputStream, cancellationToken);
            }

            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("文件 {FileName} 已直接上传到 {FilePath}，上传者: {UploaderId}", fileName, filePath, uploaderId);

            var fileMetadata = new FileMetadata(
                fileName,
                storedFileName,
                contentType,
                fileInfo.Length,
                uploaderId,
                ProviderName
           );
           fileMetadata.ConfirmUpload(uploaderId, null);

           return fileMetadata;
       }
        catch (Exception ex)
        {
            _logger.LogError(ex, "直接上传文件 {FileName} 失败。", fileName);
            throw;
        }
    }

    public Task DeletePhysicalFileAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            _logger.LogError("尝试删除物理文件失败：storedFileName 为空。");
            throw new ArgumentNullException(nameof(storedFileName), "Stored file name cannot be null or whitespace.");
        }

        var filePath = Path.Combine(_storagePath, storedFileName);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("物理文件 {FilePath} 已从本地存储删除。", filePath);
            }
            else
            {
                _logger.LogWarning("尝试删除物理文件，但文件在本地存储中未找到: {FilePath}", filePath);
            }
            return Task.CompletedTask;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "删除物理文件 {FilePath} 时发生IO错误。", filePath);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "删除物理文件 {FilePath} 时发生权限错误。", filePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除物理文件 {FilePath} 时发生意外错误。", filePath);
            throw; 
        }
    }

    private string GetPublicUrl(string storedFileName) // Made private as it's an implementation detail
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || _baseUrl.Contains("/api/files/download"))
        {
            _logger.LogWarning("Attempting to get public URL for {StoredFileName}, but BaseUrl ('{BaseUrl}') might not be configured for direct public access. Ensure 'FileStorage:Local:BaseUrl' points to a publicly served path.",
                storedFileName, _baseUrl);
        }
        return $"{_baseUrl.TrimEnd('/')}/{HttpUtility.UrlEncode(storedFileName)}";
    }

    public Task<string?> GetPublicUrlAsync(string storedFileName, string originalFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            _logger.LogWarning("GetPublicUrlAsync called with empty storedFileName.");
            return Task.FromResult<string?>(null);
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            _logger.LogWarning("GetPublicUrlAsync called with empty originalFileName.");
            return Task.FromResult<string?>(null);
        }

        if (string.IsNullOrWhiteSpace(_publicBaseUrl))
        {
            _logger.LogWarning("GetPublicUrlAsync: PublicBaseUrl is not configured. Cannot generate a public URL for {StoredFileName}.", storedFileName);
            return Task.FromResult<string?>(null);
        }

        var encodedStoredFileName = Uri.EscapeDataString(storedFileName);
        var encodedOriginalFileName = Uri.EscapeDataString(originalFileName);

        var publicUrl = $"{_publicBaseUrl.TrimEnd('/')}/{encodedStoredFileName}/{encodedOriginalFileName}";

        _logger.LogInformation("Generated public URL for {StoredFileName} (Original: {OriginalFileName}): {PublicUrl}",
            storedFileName, originalFileName, publicUrl);

        return Task.FromResult<string?>(publicUrl);
    }

    public async Task<string?> GetPublicUrlAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Deprecated GetPublicUrlAsync(storedFileName) called. Consider using GetPublicUrlAsync(storedFileName, originalFileName) instead.");
        return await Task.FromResult<string?>(GetPublicUrl(storedFileName));
    }

    public async Task<Result<FileDownloadInfo>> GetFileStreamViaTokenAsync(
        string targetFile,
        Guid userId,
        string token,
        string expiresQueryParam,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetFileStreamViaTokenAsync: Attempting to get stream for TargetFile={TargetFile}, UserId={UserId}, Expires={ExpiresQueryParam}",
            targetFile, userId, expiresQueryParam);

        if (string.IsNullOrWhiteSpace(targetFile) ||
            userId == Guid.Empty ||
            string.IsNullOrWhiteSpace(token) ||
            !DateTime.TryParse(expiresQueryParam, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expirationTime))
        {
            _logger.LogWarning("GetFileStreamViaTokenAsync: Invalid parameters.");
            return Result<FileDownloadInfo>.Failure("File.DownloadByToken.InvalidParams", "Request parameters are invalid or incomplete.");
        }

        if (expirationTime < DateTime.UtcNow)
        {
            _logger.LogWarning("GetFileStreamViaTokenAsync: Token expired at {ExpirationTime} for TargetFile={TargetFile}", expirationTime, targetFile);
            return Result<FileDownloadInfo>.Failure("File.DownloadByToken.Expired", "Download link has expired.");
        }

        var payload = $"{targetFile}|{userId}|{expirationTime:o}";
        if (!ValidateHmacToken(payload, token, _uploadTokenSecret))
        {
            _logger.LogWarning("GetFileStreamViaTokenAsync: Invalid token for TargetFile={TargetFile}. Payload={Payload}", targetFile, payload);
            return Result<FileDownloadInfo>.Failure("File.DownloadByToken.InvalidToken", "Invalid download link or token.");
        }

        var filePath = Path.Combine(_storagePath, targetFile);

        if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(_storagePath), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("GetFileStreamViaTokenAsync: Potential path traversal detected. TargetFile={TargetFile}, ResolvedPath={FilePath}, StoragePath={StoragePath}", targetFile, filePath, _storagePath);
            return Result<FileDownloadInfo>.Failure("File.InvalidPath", "Invalid file name or path.");
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("GetFileStreamViaTokenAsync: File {FilePath} not found.", filePath);
            return Result<FileDownloadInfo>.Failure("File.NotFound", "The requested file does not exist.");
        }

        try
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

            string originalFileName = targetFile; // Assuming originalFileName is the same as targetFile for simplicity
            string actualContentType = "application/octet-stream"; // Default content type

            _logger.LogInformation("GetFileStreamViaTokenAsync: Successfully prepared stream for TargetFile={TargetFile} (Original: {OriginalFileName}, Type: {ContentType})",
                targetFile, originalFileName, actualContentType);
            return Result<FileDownloadInfo>.Success(new FileDownloadInfo(fileStream, actualContentType, originalFileName));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "GetFileStreamViaTokenAsync: File {FilePath} not found when attempting to open stream.", filePath);
            return Result<FileDownloadInfo>.Failure("File.NotFound", "The requested file does not exist.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetFileStreamViaTokenAsync: Error opening file stream for {FilePath}.", filePath);
            return Result<FileDownloadInfo>.Failure("File.DownloadByToken.StorageError", "An internal error occurred while accessing the file.");
        }
    }

    public Task<Result<FileDownloadInfo>> GetFileStreamAsync(
        string storedFileName,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetFileStreamAsync: Attempting to get stream for StoredFileName={StoredFileName}, OriginalFileName={OriginalFileName}, ContentType={ContentType}",
            storedFileName, originalFileName, contentType);

        if (string.IsNullOrWhiteSpace(storedFileName) ||
            string.IsNullOrWhiteSpace(originalFileName) ||
            string.IsNullOrWhiteSpace(contentType))
        {
            _logger.LogWarning("GetFileStreamAsync: Invalid parameters. StoredFileName, OriginalFileName, or ContentType is null or whitespace.");
            return Task.FromResult(Result<FileDownloadInfo>.Failure("File.Download.InvalidParams", "Required file information (stored name, original name, content type) is missing."));
        }

        var filePath = Path.Combine(_storagePath, storedFileName);

        if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(_storagePath), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("GetFileStreamAsync: Potential path traversal detected. StoredFileName={StoredFileName}, ResolvedPath={FilePath}, StoragePath={StoragePath}", storedFileName, filePath, _storagePath);
            return Task.FromResult(Result<FileDownloadInfo>.Failure("File.InvalidPath", "Invalid file name or path."));
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("GetFileStreamAsync: File {FilePath} (Stored: {StoredFileName}) not found.", filePath, storedFileName);
            return Task.FromResult(Result<FileDownloadInfo>.Failure("File.PhysicalNotFound", "The requested physical file does not exist."));
        }

        try
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            
            _logger.LogInformation("GetFileStreamAsync: Successfully prepared stream for StoredFileName={StoredFileName} (Original: {OriginalFileName})",
                storedFileName, originalFileName);
            
            var downloadInfo = new FileDownloadInfo(fileStream, contentType, originalFileName);
            return Task.FromResult(Result<FileDownloadInfo>.Success(downloadInfo));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "GetFileStreamAsync: File {FilePath} not found when attempting to open stream (Stored: {StoredFileName}).", filePath, storedFileName);
            return Task.FromResult(Result<FileDownloadInfo>.Failure("File.PhysicalNotFound", "The requested physical file does not exist."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetFileStreamAsync: Error opening file stream for {FilePath} (Stored: {StoredFileName}).", filePath, storedFileName);
            return Task.FromResult(Result<FileDownloadInfo>.Failure("File.Download.StorageError", "An internal error occurred while accessing the file."));
        }
    }

    public string GetPhysicalFilePath(string storedFileName)
    {
        return Path.Combine(_storagePath, storedFileName);
    }

    public string GetStorageRootPath()
    {
        return _storagePath;
    }

    private string GetApplicationBaseUrl()
    {
        if (!string.IsNullOrEmpty(_appBaseUrl))
        {
            return _appBaseUrl.TrimEnd('/');
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            return $"{request.Scheme}://{request.Host}";
        }

        _logger.LogWarning("ApplicationSettings.BaseUrl is not configured and HttpContext is not available. Returning default 'http://localhost'. Ensure ApplicationSettings.BaseUrl is configured for production environments.");
        return "http://localhost"; // Or return string.Empty or throw an exception based on requirements
    }

    public async Task<Result> SaveUploadedFileViaTokenAsync(
        string targetFile,
        string contentType,
        string token,
        string expiresQueryParam,
        Stream requestBody,
        long? fileSize, // fileSize is currently not used in the core saving logic but kept for interface consistency
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SaveUploadedFileViaTokenAsync: Attempting to save TargetFile={TargetFile}, ContentType={ContentType}, Expires={ExpiresQueryParam}, Size={FileSize}",
            targetFile, contentType, expiresQueryParam, fileSize);

        if (string.IsNullOrWhiteSpace(targetFile) ||
            string.IsNullOrWhiteSpace(contentType) ||
            string.IsNullOrWhiteSpace(token) ||
            !DateTime.TryParse(expiresQueryParam, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expirationTime))
        {
            _logger.LogWarning("SaveUploadedFileViaTokenAsync: Invalid parameters.");
            return Result.Failure("File.UploadByToken.InvalidParams", "Request parameters are invalid or incomplete.");
        }

        if (expirationTime < DateTime.UtcNow)
        {
            _logger.LogWarning("SaveUploadedFileViaTokenAsync: Token expired at {ExpirationTime} for TargetFile={TargetFile}", expirationTime, targetFile);
            return Result.Failure("File.UploadByToken.Expired", "Upload link has expired.");
        }

        var payload = $"{targetFile}|{contentType}|{expirationTime:o}";
        if (!ValidateHmacToken(payload, token, _uploadTokenSecret))
        {
            _logger.LogWarning("SaveUploadedFileViaTokenAsync: Invalid token for TargetFile={TargetFile}. Payload={Payload}", targetFile, payload);
            return Result.Failure("File.UploadByToken.InvalidToken", "Invalid upload link or token.");
        }

        var filePath = Path.Combine(_storagePath, targetFile);

        // Prevent path traversal attacks
        if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(_storagePath), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("SaveUploadedFileViaTokenAsync: Potential path traversal detected. TargetFile={TargetFile}, ResolvedPath={FilePath}, StoragePath={StoragePath}", targetFile, filePath, _storagePath);
            return Result.Failure("File.InvalidPath", "Invalid file name or path.");
        }
        
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogInformation("SaveUploadedFileViaTokenAsync: Created directory {DirectoryPath}", directoryPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveUploadedFileViaTokenAsync: Failed to create storage directory {DirectoryPath} for TargetFile={TargetFile}", directoryPath, targetFile);
                return Result.Failure("File.Storage.DirectoryError", "Could not create storage directory.");
            }
        }

        try
        {
            // FileMode.Create will overwrite if the file exists.
            // Consider business logic for handling existing files if overwrite is not desired.
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await requestBody.CopyToAsync(fileStream, cancellationToken);
            }
            _logger.LogInformation("SaveUploadedFileViaTokenAsync: File {TargetFile} successfully saved to {FilePath}", targetFile, filePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveUploadedFileViaTokenAsync: Error saving file {TargetFile} to {FilePath}.", targetFile, filePath);
            // Attempt to delete partially written file if an error occurs
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); }
                catch (Exception deleteEx) { _logger.LogError(deleteEx, "SaveUploadedFileViaTokenAsync: Failed to delete partially uploaded file {FilePath} after error.", filePath); }
            }
            return Result.Failure("File.UploadByToken.StorageError", "An internal error occurred while saving the file.");
        }
    }
}