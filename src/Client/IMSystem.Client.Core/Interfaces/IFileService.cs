using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.Files;
using IMSystem.Protocol.DTOs.Responses.Files;
using System.IO;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles file operations.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Requests permission from the server to upload a file.
        /// </summary>
        /// <param name="request">The request details for file upload.</param>
        /// <returns>A result containing the server's response, including the pre-signed URL for uploading.</returns>
        Task<Result<RequestFileUploadResponse>> RequestFileUploadAsync(RequestFileUploadRequest request);

        /// <summary>
        /// Confirms that a file upload has been completed.
        /// </summary>
        /// <param name="request">The request details for confirming the file upload.</param>
        /// <returns>A result containing the server's response after confirmation.</returns>
        Task<Result<ConfirmFileUploadResponse>> ConfirmFileUploadAsync(ConfirmFileUploadRequest request);

        /// <summary>
        /// Downloads a file from the server.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file to download.</param>
        /// <returns>A result containing the file stream if successful.</returns>
        Task<Result<Stream>> DownloadFileAsync(string fileId);
/// <summary>
        /// Deletes a file from the server.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file to delete.</param>
        /// <returns>A result indicating the success or failure of the operation.</returns>
        Task<Result> DeleteFileAsync(string fileId);
    }
}