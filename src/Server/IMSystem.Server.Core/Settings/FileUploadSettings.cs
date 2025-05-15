using System.Collections.Generic;

namespace IMSystem.Server.Core.Settings
{
    /// <summary>
    /// 文件上传相关配置。
    /// </summary>
    public class FileUploadSettings
    {
        /// <summary>
        /// 允许上传的最大文件大小（字节）。
        /// </summary>
        public long MaxFileSize { get; set; }

        /// <summary>
        /// 允许的文件内容类型列表。
        /// </summary>
        public List<string> AllowedContentTypes { get; set; } = new List<string>();

        /// <summary>
        /// 预签名上传 URL 过期时间分钟数（可选）。
        /// </summary>
        public int UploadTokenExpirationMinutes { get; set; }

        /// <summary>
        /// 预签名下载 URL 过期时间分钟数（可选）。
        /// </summary>
        public int DownloadTokenExpirationMinutes { get; set; }
    }
}