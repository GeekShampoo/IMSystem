using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Protocol.DTOs.Responses.Auth; // For LoginResponse
using IMSystem.Protocol.Common; // For Result
using MediatR;
using Microsoft.Extensions.Logging; // For logging

namespace IMSystem.Server.Core.Features.Authentication.Commands
{
    /// <summary>
    /// 处理用户登录命令的处理器。
    /// </summary>
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse?>> // Changed return type
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ILogger<LoginCommandHandler> _logger;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork; // Added IUnitOfWork

        /// <summary>
        /// 初始化 <see cref="LoginCommandHandler"/> 类的新实例。
        /// </summary>
        /// <param name="userRepository">用户仓储，用于获取用户信息。</param>
        /// <param name="jwtTokenGenerator">JWT Token 生成器，用于生成 Token。</param>
        /// <param name="logger">日志记录器，用于记录登录过程中的信息。</param>
        /// <param name="passwordHasher">密码哈希服务。</param>
        /// <param name="unitOfWork">工作单元。</param>
        public LoginCommandHandler(
            IUserRepository userRepository,
            IJwtTokenGenerator jwtTokenGenerator,
            ILogger<LoginCommandHandler> logger,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork // Added IUnitOfWork
            )
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork)); // Added IUnitOfWork
        }

        /// <summary>
        /// 异步处理用户登录命令。
        /// </summary>
        /// <param name="request">登录命令，包含用户名和密码。</param>
        /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
        /// <returns>表示异步操作的任务。任务结果包含 <see cref="LoginResponse"/> DTO，如果登录成功；否则返回失败的 Result。</returns>
        public async Task<Result<LoginResponse?>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUsernameWithProfileAsync(request.Username);

            if (user == null)
            {
                _logger.LogWarning("用户 {Username} 登录失败：用户未找到。", request.Username);
                return Result<LoginResponse?>.Failure("Auth.UserNotFound", "用户名或密码错误。");
            }
            
            if (user.IsDeactivated)
            {
                _logger.LogWarning("用户 {Username} 登录失败：账户已停用。", request.Username);
                return Result<LoginResponse?>.Failure("Auth.AccountDeactivated", "您的账户已被停用。");
            }

            bool passwordIsValid = _passwordHasher.VerifyHashedPassword(user.PasswordHash, request.Password);

            if (!passwordIsValid)
            {
                _logger.LogWarning("用户 {Username} 登录失败：密码无效。", request.Username);
                return Result<LoginResponse?>.Failure("Auth.InvalidPassword", "用户名或密码错误。");
            }

            // Update last login info
            user.UpdateLoginInfo(request.IpAddress);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 密码有效，生成 Token
            var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

            _logger.LogInformation("用户 {Username} 登录成功。IP: {IpAddress}", user.Username, request.IpAddress ?? "N/A");

            var response = new LoginResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Token = token,
                ExpiresAt = expiresAt,
                Email = user.Email,
                ProfilePictureUrl = user.Profile?.AvatarUrl
            };
            return Result<LoginResponse?>.Success(response);
        }
    }
}