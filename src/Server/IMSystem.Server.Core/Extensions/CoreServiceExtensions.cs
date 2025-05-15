using FluentValidation;
// using FluentValidation.DependencyInjectionExtensions; // This namespace might not be needed if the extension method is correctly picked up
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using IMSystem.Server.Core.Behaviors; // Assuming MediatR behaviors will be here
using IMSystem.Server.Core.Interfaces.Services; // Added for IPermissionService
using IMSystem.Server.Core.Services;             // Added for PermissionService

namespace IMSystem.Server.Core.Extensions
{
    /// <summary>
    /// 包含用于配置核心服务的扩展方法。
    /// </summary>
    public static class CoreServiceExtensions
    {
        /// <summary>
        /// 将核心服务添加到指定的 <see cref="IServiceCollection"/>。
        /// </summary>
        /// <param name="services">要添加服务的 <see cref="IServiceCollection"/>。</param>
        /// <returns>修改后的 <see cref="IServiceCollection"/>。</returns>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // MediatR
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                // Register MediatR pipeline behaviors
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)); // 添加日志行为
                // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            });


            // FluentValidation - Register all validators from the assembly
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Register other Core services if any (e.g., domain services that need DI)
            services.AddScoped<IPermissionService, PermissionService>();

            return services;
        }
    }
}