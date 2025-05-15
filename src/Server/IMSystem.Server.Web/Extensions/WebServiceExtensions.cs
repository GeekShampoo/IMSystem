using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Infrastructure.BackgroundServices;
using IMSystem.Server.Infrastructure.Identity;
using IMSystem.Server.Web.Hubs;
using IMSystem.Server.Web.Interfaces;
using IMSystem.Server.Web.Profiles;
using IMSystem.Server.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Hosting; // Added for IWebHostEnvironment
using Microsoft.Extensions.Hosting; // Added for IsDevelopment()
using System.Text;
using System.Security.Claims;

namespace IMSystem.Server.Web.Extensions
{
    public static class WebServiceExtensions
    {
        public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // 注册 Web 层服务
            services.AddScoped<INotificationService, ChatNotificationService>();
            services.AddScoped<IChatNotificationService, ChatNotificationService>(); // Re-add IChatNotificationService registration
            services.AddScoped<ISignalRConnectionService, SignalRConnectionService>(); // 注册SignalR连接服务
            services.AddSingleton<IUserConnectionManager, UserConnectionManager>(); // 注册用户连接管理器（单例模式）

            // AutoMapper 配置
            services.AddAutoMapper(typeof(MappingProfile).Assembly);

            // 控制器配置
            services.AddHttpContextAccessor();
            services.AddControllers();

            // SignalR 配置
            services.AddSignalR();

            // OpenAPI / Swagger 配置
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "IMSystem API", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "请在字段中输入带有 Bearer 的 JWT",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }});
                // 配置 Swagger 以使用 XML 注释
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            // 身份验证和授权配置
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
            if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Secret))
            {
                throw new InvalidOperationException("JWT 配置未正确设置。");
            }
            var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Require Https Metadata should be true in production.
                // It can be set to false in development if HTTPS is not used locally.
                options.RequireHttpsMetadata = configuration.GetValue<bool>("JwtSettings:RequireHttpsMetadata", defaultValue: true);
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/hubs/messaging") || path.StartsWithSegments("/hubs/presence")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();

            // CORS 配置
            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", policy =>
                {
                    var allowedOrigins = configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();

                    if (environment.IsDevelopment())
                    {
                        if (allowedOrigins != null && allowedOrigins.Length > 0)
                        {
                            policy.WithOrigins(allowedOrigins);
                        }
                        else
                        {
                            // Default origins for local development if not specified in appsettings.Development.json
                            policy.WithOrigins("http://localhost:3000", "https://localhost:3001", "http://localhost:5173"); // Added common Vite/React dev port
                            // Consider logging a warning if relying on these hardcoded defaults in dev.
                        }
                        policy.AllowAnyHeader();
                        policy.AllowAnyMethod();
                    }
                    else // Production or other non-development environments
                    {
                        if (allowedOrigins != null && allowedOrigins.Length > 0)
                        {
                            policy.WithOrigins(allowedOrigins)
                                  // 生产环境下仅允许必要的Header和Method，禁止AllowAnyHeader/AllowAnyMethod，提升安全性
                                  .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept", "Origin")
                                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS");
                            // 如需调整允许的Header或Method，请根据实际业务需求修改上方列表
                        }
                        else
                        {
                            // CRITICAL: No CORS origins configured for this environment in appsettings (CorsSettings:AllowedOrigins).
                            // This policy will NOT allow any cross-origin requests by default.
                            // You MUST configure the allowed origins for your client applications in production.
                            // Example in appsettings.Production.json:
                            // "CorsSettings": { "AllowedOrigins": ["https://your-production-client.com"] }
                            // policy.WithOrigins("https://your-default-placeholder.com"); // Or some other restrictive measure
                            // Consider logging a critical error here if no origins are set for production.
                        }
                    }
                    policy.AllowCredentials();
                });
            });

            // 托管服务 (OutboxProcessorService 已经在 Infrastructure 层注册，这里不需要重复)
            // services.AddHostedService<OutboxProcessorService>(); // 已在 InfrastructureServiceExtensions 中注册

            return services;
        }
    }
}

// 用户ID统一获取扩展方法
namespace IMSystem.Server.Web.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// 从ClaimsPrincipal安全获取用户ID（Guid），无效时返回null。
        /// </summary>
        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            var id = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : (Guid?)null;
        }
    }
}