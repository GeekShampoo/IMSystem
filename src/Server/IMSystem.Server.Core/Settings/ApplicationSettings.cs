using System;
using System.Collections.Generic;

namespace IMSystem.Server.Core.Settings
{
    /// <summary>
    /// 应用程序的核心配置设置
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// 应用程序的基础URL，用于构建完整的URL（如邮件验证链接等）
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// API 相关的URL路径配置
        /// </summary>
        public ApiUrlSettings ApiUrls { get; set; } = new ApiUrlSettings();
    }

    /// <summary>
    /// API URL路径配置
    /// </summary>
    public class ApiUrlSettings
    {
        /// <summary>
        /// 文件上传相关URL配置
        /// </summary>
        public FileApiUrlSettings Files { get; set; } = new FileApiUrlSettings();
        
        /// <summary>
        /// 用户相关URL配置
        /// </summary>
        public UserApiUrlSettings User { get; set; } = new UserApiUrlSettings();
    }

    /// <summary>
    /// 文件相关的API URL路径配置
    /// </summary>
    public class FileApiUrlSettings
    {
        /// <summary>
        /// 文件上传令牌端点的相对路径
        /// </summary>
        public string UploadTokenEndpoint { get; set; } = "/api/files/upload-by-token";
        
        /// <summary>
        /// 文件下载令牌端点的相对路径
        /// </summary>
        public string DownloadTokenEndpoint { get; set; } = "/api/files/download-by-token";
    }
    
    /// <summary>
    /// 用户相关的API URL路径配置
    /// </summary>
    public class UserApiUrlSettings
    {
        /// <summary>
        /// 邮件验证页面的相对路径
        /// </summary>
        public string EmailVerificationPath { get; set; } = "/verify-email";
    }
}