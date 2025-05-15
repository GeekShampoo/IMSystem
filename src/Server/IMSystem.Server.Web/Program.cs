using IMSystem.Server.Core.Extensions;
using IMSystem.Server.Infrastructure.BackgroundServices; // 用于 OutboxProcessorService
using IMSystem.Server.Infrastructure.Extensions;
using IMSystem.Server.Web.Extensions; // 新增: 引用 Web 服务扩展
using IMSystem.Server.Web.Hubs; // 用于 MessagingHub, PresenceHub
using IMSystem.Server.Web.Middleware; // Added for GlobalExceptionHandlerMiddleware

[assembly: Microsoft.AspNetCore.Mvc.ApiConventionType(typeof(IMSystem.Server.Web.DefaultApiConventions))]

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// 将服务添加到容器。
builder.Services.AddCoreServices();
builder.Services.AddInfrastructureServices(configuration);
builder.Services.AddWebServices(configuration, builder.Environment); // 新增: 调用 Web 服务扩展方法, 传入 Environment

// 注册 OutboxMessageProcessorService 作为托管服务
builder.Services.AddHostedService<OutboxMessageProcessorService>();


var app = builder.Build();

// 配置 HTTP 请求管道。
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IMSystem API v1"));
    // app.UseDeveloperExceptionPage(); // Replaced by GlobalExceptionHandlerMiddleware
}
else
{
    // app.UseExceptionHandler("/Error"); // Replaced by GlobalExceptionHandlerMiddleware
    app.UseHsts();
}

app.UseGlobalExceptionHandler(); // Add our custom global exception handler

app.UseHttpsRedirection();

app.UseCors("DefaultCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MessagingHub>("/hubs/messaging");
app.MapHub<PresenceHub>("/hubs/presence");
app.MapHub<SignalingHub>("/hubs/signaling");
app.Run();
