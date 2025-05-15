using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Infrastructure.Caching;
using IMSystem.Server.Infrastructure.Configuration; // 添加这一行
using IMSystem.Server.Infrastructure.FileStorage;
using IMSystem.Server.Infrastructure.Identity;
using IMSystem.Server.Infrastructure.Persistence;
using IMSystem.Server.Infrastructure.Persistence.Repositories;
using IMSystem.Server.Infrastructure.BackgroundServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // For IConfiguration
using Microsoft.Extensions.DependencyInjection;
using IMSystem.Server.Core.Settings;
using StackExchange.Redis; // For IConnectionMultiplexer

namespace IMSystem.Server.Infrastructure.Extensions
{
    /// <summary>
    /// 基础设施层服务注册的扩展方法。
    /// </summary>
    public static class InfrastructureServiceExtensions
    {
        /// <summary>
        /// 向服务集合中添加基础设施层相关的服务。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configuration">应用程序配置。</param>
        /// <returns>配置后的服务集合。</returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 数据库上下文
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        // EnableRetryOnFailure for resilience, especially in cloud environments
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            // 仓储 & 工作单元
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IFriendshipRepository, FriendshipRepository>();
            services.AddScoped<IFriendGroupRepository, FriendGroupRepository>();
            services.AddScoped<IUserFriendGroupRepository, UserFriendGroupRepository>();
            services.AddScoped<IOutboxRepository, OutboxRepository>(); // 注册 IOutboxRepository
            services.AddScoped<IGroupRepository, GroupRepository>(); // 注册 IGroupRepository
            services.AddScoped<IMessageReadReceiptRepository, MessageReadReceiptRepository>(); // 注册 IMessageReadReceiptRepository
            services.AddScoped<IFileMetadataRepository, FileMetadataRepository>(); // 注册 IFileMetadataRepository
            services.AddScoped<IGroupInvitationRepository, GroupInvitationRepository>(); // 注册 IGroupInvitationRepository
            services.AddScoped<IGroupMemberRepository, GroupMemberRepository>(); // 注册 IGroupMemberRepository
            services.AddScoped<IUserProfileRepository, UserProfileRepository>(); // Added from DependencyInjection.cs
            // services.AddScoped<IUserBlockRepository, UserBlockRepository>(); // 已移除注册
            // 在此处添加其他仓储...
            // ... 等等

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 缓存服务 (Redis)
            var redisConnectionString = configuration.GetConnectionString("RedisConnection");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
                    configurationOptions.AbortOnConnectFail = false;
                    return ConnectionMultiplexer.Connect(configurationOptions);
                });
                services.AddSingleton<ICachingService, RedisCachingService>();
            }
            else
            {
                // 如果 Redis 未配置，则回退或记录警告
                // services.AddSingleton<ICachingService, InMemoryCachingService>(); // 示例回退
                // 目前，如果未配置，ICachingService 将不会注册，如果使用会导致 DI 错误。
            }

            // 文件存储服务
            services.Configure<LocalFileStorageOptions>(configuration.GetSection("FileStorage:Local"));
            services.Configure<FileUploadSettings>(configuration.GetSection("FileUploadSettings"));
            services.Configure<MessageSettings>(configuration.GetSection("MessageSettings"));
            services.Configure<UserSettings>(configuration.GetSection("UserSettings"));
            services.Configure<FileCleanupSettings>(configuration.GetSection("BackgroundServices:FileCleanup"));
            services.Configure<OutboxProcessorSettings>(configuration.GetSection("BackgroundServices:OutboxProcessor"));
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            // 对于云存储，类似地配置和注册：
            // services.Configure<AzureBlobStorageOptions>(configuration.GetSection("FileStorage:AzureBlob"));
            // services.AddScoped<IFileStorageService, AzureBlobStorageService>();

            // 身份服务 (JWT)
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IPasswordHasher, PasswordHasher>(); // 注册密码哈希器

            // 注册其他基础设施服务

            // 注册文件清理后台服务
            services.AddHostedService<FileCleanupBackgroundService>();

            return services;
        }
    }
}