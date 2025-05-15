using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Infrastructure.Configuration; // 添加这一行
using IMSystem.Server.Infrastructure.FileStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // 添加这一行
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Infrastructure.BackgroundServices;

/// <summary>
/// 定期清理超时未确认上传和孤立物理文件的后台服务。
/// </summary>
public class FileCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileCleanupBackgroundService> _logger;
    private readonly FileCleanupSettings _settings;

    public FileCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<FileCleanupBackgroundService> logger, IOptions<FileCleanupSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var fileMetadataRepo = scope.ServiceProvider.GetRequiredService<IFileMetadataRepository>();
                    var fileStorage = scope.ServiceProvider.GetRequiredService<LocalFileStorageService>();

                    // 1. 清理超时未确认上传
                    var timeout = DateTime.UtcNow.AddHours(-_settings.UnconfirmedUploadTimeoutHours);
                    var expiredMetadatas = await fileMetadataRepo.GetUnconfirmedUploadsOlderThanAsync(timeout, stoppingToken);
                    foreach (var meta in expiredMetadatas)
                    {
                        try
                        {
                            var filePath = fileStorage.GetPhysicalFilePath(meta.StoredFileName);
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                                _logger.LogInformation("已删除超时未确认上传的物理文件: {File}", filePath);
                            }
                            await fileMetadataRepo.RemoveAsync(meta, stoppingToken);
                            _logger.LogInformation("已删除超时未确认上传的元数据: {Id}", meta.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "清理超时未确认上传文件时出错: {FileName}", meta.StoredFileName);
                        }
                    }

                    // 2. 清理孤立物理文件
                    var allMetadatas = await fileMetadataRepo.GetAllAsync();
                    var validFileNames = allMetadatas.Select(m => m.StoredFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var storagePath = fileStorage.GetStorageRootPath();
                    var files = Directory.GetFiles(storagePath);
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        if (!validFileNames.Contains(fileName))
                        {
                            try
                            {
                                File.Delete(file);
                                _logger.LogInformation("已删除孤立物理文件: {File}", file);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "删除孤立物理文件时出错: {File}", file);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件清理后台服务执行异常");
            }

            await Task.Delay(TimeSpan.FromHours(_settings.IntervalHours), stoppingToken);
        }
    }
}