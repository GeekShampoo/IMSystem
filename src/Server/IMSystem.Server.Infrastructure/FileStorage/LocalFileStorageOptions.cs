namespace IMSystem.Server.Infrastructure.FileStorage;

/// <summary>
/// 本地文件存储服务的配置选项。
/// </summary>
public class LocalFileStorageOptions
{
    /// <summary>
    /// 配置节的名称，通常在 appsettings.json 中定义。
    /// </summary>
    public const string SectionName = "FileStorage:Local";

    /// <summary>
    /// 获取或设置文件存储的根路径。
    /// 如果未提供，将使用默认路径（例如，应用程序根目录下的 "uploads" 文件夹）。
    /// </summary>
    public string? StoragePath { get; set; }

    /// <summary>
    /// 获取或设置用于构造可访问文件URL的基础URL。
    /// 例如："https://yourdomain.com/files" 或相对路径 "/files"。
    /// 如果未提供，服务可能会尝试从当前HTTP上下文中动态构建，或者使用默认值。
    /// </summary>
    public string? BaseUrl { get; set; } // This is for token-based downloads or internal use
    
    /// <summary>
    /// 获取或设置用于构造可公开访问文件URL的基础URL，该URL应指向静态文件服务器。
    /// 例如："https://static.yourdomain.com/files"。
    /// 如果此项未配置，GetPublicUrlAsync 将可能无法生成有效的公共URL。
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// 获取或设置用于生成和验证上传令牌的密钥。
    /// 强烈建议在生产环境中使用强密钥，并从安全配置中加载。
    /// </summary>
    public string? UploadTokenSecret { get; set; }

    /// <summary>
    /// 获取或设置上传令牌的有效分钟数。默认为30分钟。
    /// </summary>
    public int UploadTokenExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 获取或设置下载令牌的有效分钟数。默认为60分钟。
    /// </summary>
    public int DownloadTokenExpirationMinutes { get; set; } = 60;
}