using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.Files;
using IMSystem.Protocol.DTOs.Responses.Files;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Services
{
    /// <summary>
    /// Provides services for file operations, such as uploading and downloading.
    /// </summary>
    public class FileService : IFileService
    {
        private readonly IApiService _apiService;

        public FileService(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        /// <inheritdoc />
        public async Task<Result<RequestFileUploadResponse>> RequestFileUploadAsync(RequestFileUploadRequest request)
        {
            if (request == null)
            {
                return Result<RequestFileUploadResponse>.Failure(new Error("RequestFileUpload.NullRequest", "Request cannot be null."));
            }
            return await _apiService.PostAsync<RequestFileUploadRequest, Result<RequestFileUploadResponse>>("api/Files/request-upload", request);
        }

        /// <inheritdoc />
        public async Task<Result<ConfirmFileUploadResponse>> ConfirmFileUploadAsync(ConfirmFileUploadRequest request)
        {
            if (request == null)
            {
                return Result<ConfirmFileUploadResponse>.Failure(new Error("ConfirmFileUpload.NullRequest", "Request cannot be null."));
            }
            return await _apiService.PostAsync<ConfirmFileUploadRequest, Result<ConfirmFileUploadResponse>>("api/Files/confirm-upload", request);
        }

        /// <inheritdoc />
        public async Task<Result<Stream>> DownloadFileAsync(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                return Result<Stream>.Failure(new Error("DownloadFile.InvalidFileId", "File ID cannot be null or whitespace."));
            }

            try
            {
                var stream = await _apiService.GetAsync<Stream>($"/api/Files/download/{fileId}");
                if (stream != null && stream.CanRead)
                {
                    return Result<Stream>.Success(stream);
                }
                else
                {
                    return Result<Stream>.Failure(new Error("DownloadFile.Failed", "Failed to download file or stream was not readable."));
                }
            }
            catch (HttpRequestException ex)
            {
                return Result<Stream>.Failure(new Error("DownloadFile.HttpRequestError", $"HTTP request failed: {ex.Message}"));
            }
            catch (Exception ex)
            {
                return Result<Stream>.Failure(new Error("DownloadFile.UnexpectedError", $"An unexpected error occurred: {ex.Message}"));
            }
        }
/// <inheritdoc />
        public async Task<Result> DeleteFileAsync(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                return Result.Failure(new Error("DeleteFile.InvalidFileId", "File ID cannot be null or whitespace."));
            }

            try
            {
                await _apiService.DeleteAsync($"/api/Files/{fileId}");
                return Result.Success();
            }
            catch (ApiException ex)
            {
                return Result.Failure(ex.Error.ErrorCode ?? "ApiError", ex.Error.Title ?? ex.Message);
            }
            catch (Exception ex)
            {
                return Result.Failure("DeleteFile.UnexpectedError", $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}