using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.DTOs.Notifications.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Core.Settings;
using IMSystem.Server.Domain.Events.User;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IMSystem.Protocol.Common; // Added for SignalRClientMethods

namespace IMSystem.Server.Core.Features.User.EventHandlers
{
    /// <summary>
    /// 处理用户注册事件，发送电子邮件验证链接
    /// </summary>
    public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
    {
        private readonly ILogger<UserRegisteredEventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly string _appBaseUrl;
        private readonly string _emailVerificationPath;

        public UserRegisteredEventHandler(
            ILogger<UserRegisteredEventHandler> logger,
            INotificationService notificationService,
            IUserRepository userRepository,
            IOptions<ApplicationSettings> appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            
            var applicationSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings), "ApplicationSettings cannot be null.");
            _appBaseUrl = applicationSettings.BaseUrl;
            if (string.IsNullOrWhiteSpace(_appBaseUrl))
            {
                _logger.LogWarning("ApplicationSettings.BaseUrl 未配置。这对于生成正确的电子邮件验证URL是必需的。");
            }
            _emailVerificationPath = applicationSettings.ApiUrls.User.EmailVerificationPath;
        }

        public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("处理 UserRegisteredEvent，用户ID: {UserId}, 邮箱: {Email}", notification.UserId, notification.Email);

            try
            {
                // 1. 获取用户的验证令牌
                var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("无法获取用户信息，用户ID: {UserId}", notification.UserId);
                    return;
                }

                if (string.IsNullOrEmpty(user.EmailVerificationToken))
                {
                    _logger.LogWarning("用户 {UserId} 没有电子邮件验证令牌", notification.UserId);
                    return;
                }

                // 2. 构建验证URL
                string baseUrl = _appBaseUrl;
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    baseUrl = "https://example.com"; // 此为回退值，正式环境应确保配置正确的BaseUrl
                    _logger.LogError("ApplicationSettings.BaseUrl 未配置，将使用临时占位符 {PlaceholderUrl}。这在生产环境中是不可接受的。", baseUrl);
                }
                
                string verificationUrl = $"{baseUrl.TrimEnd('/')}{_emailVerificationPath}?token={user.EmailVerificationToken}";

                // 3. 准备邮件内容，使用规范化的DTO
                var emailPayload = new EmailNotificationPayloadDto
                {
                    To = user.Email,
                    Subject = "IMSystem - 验证您的电子邮箱",
                    Body = $@"
                        <html>
                        <body>
                            <h2>欢迎加入 IMSystem!</h2>
                            <p>亲爱的 {user.Username},</p>
                            <p>感谢您注册 IMSystem。请点击下面的链接验证您的电子邮箱:</p>
                            <p><a href='{verificationUrl}'>{verificationUrl}</a></p>
                            <p>该链接将在 {user.EmailVerificationTokenExpiresAt?.ToString("yyyy-MM-dd HH:mm:ss")} 过期。</p>
                            <p>如果您没有注册 IMSystem，请忽略此邮件。</p>
                            <p>谢谢,<br/>IMSystem 团队</p>
                        </body>
                        </html>",
                    IsHtml = true
                };

                // 4. 发送邮件通知
                // 这里使用通用通知服务发送邮件，方法名为"SendEmail"
                await _notificationService.SendNotificationAsync("system", SignalRClientMethods.SendEmail, emailPayload);

                _logger.LogInformation("已成功发送验证邮件到 {Email}，用户ID: {UserId}", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理用户注册事件时发生错误，用户ID: {UserId}", notification.UserId);
                // 不要抛出异常，以免阻止其他事件处理
            }
        }
    }
}