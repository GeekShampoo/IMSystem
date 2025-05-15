using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Required for ILogger
using System;
using System.IO;

namespace IMSystem.Server.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating instances of <see cref="ApplicationDbContext"/>.
/// This is used by EF Core tools (e.g., for migrations) when the application's
/// service provider cannot be accessed or configured easily at design time.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        // It's common to try and load appsettings.json or appsettings.Development.json
        // to get the connection string, similar to how the application does at runtime.
        // The base path should point to the startup project (IMSystem.Server.Web)
        // where appsettings.json is located.

        // Adjust the path to where appsettings.json is located relative to this project (Infrastructure)
        // Assuming the Web project is one level up and then into src/server/IMSystem.Server.Web
        // This path might need adjustment based on your exact folder structure and where 'dotnet ef' is run from.
        // A more robust way might be to find the solution directory and navigate from there.
        // For simplicity, let's assume a common structure.

        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        // 优先支持通过环境变量 IM_DB_CONTEXT_BASEPATH 覆盖Web项目路径，便于CI/CD与多环境部署
        string? envWebProjectBasePath = Environment.GetEnvironmentVariable("IM_DB_CONTEXT_BASEPATH");
        string solutionRootPath = TryGetSolutionDirectoryPath() ?? Directory.GetCurrentDirectory();
        string webProjectBasePath = !string.IsNullOrEmpty(envWebProjectBasePath)
            ? envWebProjectBasePath
            : Path.Combine(solutionRootPath, "src", "server", "IMSystem.Server.Web");
        // 如需自定义Web项目配置路径，请设置环境变量 IM_DB_CONTEXT_BASEPATH

        Console.WriteLine($"[ApplicationDbContextFactory] Solution root determined as: {solutionRootPath}");
        Console.WriteLine($"[ApplicationDbContextFactory] Web project base path determined as: {webProjectBasePath}");
        Console.WriteLine($"[ApplicationDbContextFactory] Environment: {environment}");

        if (!Directory.Exists(webProjectBasePath))
        {
            throw new DirectoryNotFoundException($"The Web project path was not found: {webProjectBasePath}. Ensure 'dotnet ef' is run from the solution directory or the startup project directory, or adjust the path logic in ApplicationDbContextFactory.");
        }

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(webProjectBasePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Could not find a connection string named 'DefaultConnection'. Ensure appsettings.json is correctly located and configured.");
        }
        
        Console.WriteLine($"[ApplicationDbContextFactory] Using connection string: {connectionString.Substring(0, Math.Min(connectionString.Length, 50))}...");


        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString,
            sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });

        // Create a dummy logger factory and logger for the DbContext, as it expects one.
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dbContextLogger = loggerFactory.CreateLogger<ApplicationDbContext>();

        return new ApplicationDbContext(optionsBuilder.Options, dbContextLogger);
    }

    // Helper method to try and find the solution directory path
    private static string? TryGetSolutionDirectoryPath(string? currentPath = null)
    {
        var directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }
        return directory?.FullName;
    }
}