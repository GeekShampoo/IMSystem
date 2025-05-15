using AutoMapper;
using IMSystem.Protocol.Common; // For Result
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services; // 用于密码哈希等服务
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Events.User; // Added for UserRegisteredEvent
using MediatR;
using Microsoft.Extensions.Logging; // 用于日志记录
using System; // For ArgumentException
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Core.Settings;
using Microsoft.Extensions.Options;

namespace IMSystem.Server.Core.Features.User.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly UserSettings _userSettings;
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository; // Added for UserProfile
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFriendGroupRepository _friendGroupRepository; // Added for default friend group
    // private readonly IJwtTokenGenerator _jwtTokenGenerator; // 注册后是否自动登录取决于业务需求
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IPasswordHasher _passwordHasher; // 注入密码哈希服务

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository, // Added for UserProfile
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFriendGroupRepository friendGroupRepository, // Added for default friend group
        // IJwtTokenGenerator jwtTokenGenerator,
        ILogger<RegisterUserCommandHandler> logger,
        IPasswordHasher passwordHasher,
        Microsoft.Extensions.Options.IOptions<UserSettings> userSettings
        )
    {
        _userSettings = userSettings?.Value ?? throw new ArgumentNullException(nameof(userSettings));
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository; // Added for UserProfile
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _friendGroupRepository = friendGroupRepository; // Added for default friend group
        // _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("尝试注册新用户，用户名: {Username}，邮箱: {Email}", request.Username, request.Email);

        // 1. 检查用户名或邮箱是否已存在
        var existingUserByUsername = await _userRepository.GetByUsernameAsync(request.Username); // 更正方法名
        if (existingUserByUsername != null)
        {
            _logger.LogWarning("注册失败：用户名 {Username} 已存在。", request.Username);
            return Result<UserDto>.Failure("User.Register.UsernameConflict", $"用户名 '{request.Username}' 已被占用。");
        }

        var existingUserByEmail = await _userRepository.GetByEmailAsync(request.Email); // 更正方法名
        if (existingUserByEmail != null)
        {
            _logger.LogWarning("注册失败：邮箱 {Email} 已存在。", request.Email);
            return Result<UserDto>.Failure("User.Register.EmailConflict", $"邮箱 '{request.Email}' 已被注册。");
        }

        // 2. 密码哈希 (非常重要！)
        string hashedPassword = _passwordHasher.HashPassword(request.Password);


        // 3. 创建用户实体
        // 使用 User 实体的公共构造函数来创建实例
        var newUser = new Domain.Entities.User(
            request.Username,
            hashedPassword, // 应该存储哈希后的密码
            request.Email
            // profilePictureUrl 如果需要的话，可以从 request 中获取或设为默认值
        );
        // CreatedAt 会在 User 的基类 AuditableEntity/BaseEntity 的构造函数中设置

        // 4. 添加到仓储并准备保存
        await _userRepository.AddAsync(newUser);
        
        // 根据服务端开发指南，领域事件应通过 Outbox 模式处理。
        // 创建并添加 UserRegisteredEvent
        var userRegisteredEvent = new UserRegisteredEvent(newUser.Id, newUser.Username, newUser.Email, newUser.CreatedAt);
        newUser.AddDomainEvent(userRegisteredEvent);
        _logger.LogInformation("UserRegisteredEvent for UserId {UserId} added to domain events.", newUser.Id);
        // 之后，UnitOfWork 在保存更改时，会将此事件写入 Outbox 表。

        // 4.1 创建默认好友分组
        var defaultFriendGroup = new FriendGroup(
            newUser.Id,
            Constants.FriendGroupConstants.DefaultGroupName,
            Constants.FriendGroupConstants.DefaultGroupOrder,
            isDefault: true
        );
        await _friendGroupRepository.AddAsync(defaultFriendGroup);

        // 4.2 创建默认用户个人资料
        var userProfile = new UserProfile(newUser.Id, newUser.Username); // Use username as initial nickname
        await _userProfileRepository.AddAsync(userProfile);

        // 4.3 生成邮箱验证令牌
        newUser.GenerateEmailVerificationToken(TimeSpan.FromDays(_userSettings.EmailVerificationTokenExpiryDays)); // Token valid per config


        // 5. 保存所有更改 (用户, 默认好友分组, 用户个人资料, 邮箱验证令牌)
        // 将用户、默认好友分组和用户个人资料的创建合并到单个事务中
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("用户 {Username} (ID: {UserId}), 默认好友分组 '{GroupName}', 及用户个人资料已成功保存到数据库。",
                newUser.Username, newUser.Id, defaultFriendGroup.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存用户 {Username}, 其默认好友分组或用户个人资料时发生错误。", newUser.Username);
            // 根据需要，可以决定是否将特定类型的数据库异常转换为更友好的用户消息
            // 例如，处理唯一约束冲突等，尽管之前的检查应该已经捕获了用户名/邮箱重复。
            return Result<UserDto>.Failure("User.Register.StorageError", $"保存用户 {newUser.Username}, 其默认好友分组或用户个人资料时发生错误: {ex.Message}");
        }


        // 6. 映射到 UserDto 并返回
        // 注意：通常注册操作成功后，不会立即返回 JWT 令牌，除非业务流程定义了注册后自动登录。
        // 如果需要自动登录，可以在此步骤生成 JWT。
        // var token = _jwtTokenGenerator.GenerateToken(newUser.Id.ToString(), newUser.Username, newUser.Email);
        var userDto = _mapper.Map<UserDto>(newUser);
        // if (token != null) { userDto.Token = token; } // 如果自动登录，则附加令牌

        return Result<UserDto>.Success(userDto);
    }
}