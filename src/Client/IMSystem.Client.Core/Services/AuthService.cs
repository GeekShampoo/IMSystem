using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.DTOs.Requests.Auth;
using IMSystem.Protocol.DTOs.Responses.Auth;
using Microsoft.Extensions.Logging;

namespace IMSystem.Client.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApiService _apiService;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<AuthService> _logger;
        
        private string? _token;
        private DateTime _tokenExpiration;
        private Guid? _currentUserId;

        public Guid? CurrentUserId => _currentUserId;
        public string? Token => _token;
        public bool IsTokenExpired => DateTime.UtcNow >= _tokenExpiration;

        public AuthService(
            IApiService apiService,
            IDatabaseService databaseService,
            ILogger<AuthService> logger)
        {
            _apiService = apiService;
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await RestoreAuthenticationStateAsync();
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("正在尝试登录，用户名: {Username}", request.Username);
                
                // 调用API登录端点
                var response = await _apiService.PostAsync<LoginRequest, LoginResponse>("/api/Authentication/login", request);
                
                // 保存登录信息
                SaveAuthentication(response);
                
                _logger.LogInformation("登录成功，用户ID: {UserId}", response.UserId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录失败");
                throw;
            }
        }

        public void Logout()
        {
            _token = null;
            _currentUserId = null;
            _tokenExpiration = DateTime.MinValue;
            
            // 从数据库中删除令牌
            _ = RemoveTokenFromDatabaseAsync();
            
            _logger.LogInformation("用户已登出");
        }

        public void SaveAuthentication(LoginResponse response)
        {
            _token = response.Token;
            _currentUserId = response.UserId;
            
            // 解析令牌获取过期时间
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(response.Token);
            
            if (jwtToken.ValidTo != DateTime.MinValue)
            {
                _tokenExpiration = jwtToken.ValidTo;
            }
            else
            {
                // 如果无法从令牌获取过期时间，设置默认过期时间（例如1天）
                _tokenExpiration = DateTime.UtcNow.AddDays(1);
            }
            
            // 保存到数据库
            _ = SaveTokenToDatabaseAsync();
            
            _logger.LogInformation("身份验证信息已保存，令牌将于 {ExpirationTime} 过期", _tokenExpiration);
        }

        public bool IsAuthenticated()
        {
            return _currentUserId.HasValue && !string.IsNullOrEmpty(_token) && !IsTokenExpired;
        }

        private async Task SaveTokenToDatabaseAsync()
        {
            try
            {
                // 检查表是否存在，如果不存在则创建
                await _databaseService.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS AuthInfo (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId TEXT NOT NULL,
                        Token TEXT NOT NULL,
                        TokenExpiration TEXT NOT NULL
                    );");
                
                // 清除现有令牌
                await _databaseService.ExecuteAsync("DELETE FROM AuthInfo;");
                
                // 保存新令牌
                await _databaseService.ExecuteAsync(@"
                    INSERT INTO AuthInfo (UserId, Token, TokenExpiration) 
                    VALUES (@UserId, @Token, @TokenExpiration);",
                    new 
                    { 
                        UserId = _currentUserId?.ToString(), 
                        Token = _token,
                        TokenExpiration = _tokenExpiration.ToString("o")
                    });
                    _logger.LogInformation("令牌已异步保存到数据库。");
                }
            catch (Exception ex)
            {
                _logger.LogError(ex, "异步保存令牌到数据库失败");
            }
        }

        private async Task RemoveTokenFromDatabaseAsync()
        {
            try
            {
                await _databaseService.ExecuteAsync("DELETE FROM AuthInfo;");
                // 或者根据实际情况是删除所有还是特定用户的
                // await _databaseService.ExecuteAsync("DELETE FROM AuthInfo WHERE UserId = @UserId;", new { UserId = _currentUserId?.ToString() });
                _logger.LogInformation("认证信息已异步从数据库中删除。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "异步从数据库删除认证信息失败");
            }
        }

        private async Task RestoreAuthenticationStateAsync()
        {
            try
            {
                var authInfo = await _databaseService.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT UserId, Token, TokenExpiration FROM AuthInfo LIMIT 1;");
                
                if (authInfo != null)
                {
                    _token = authInfo.Token;
                    
                    if (Guid.TryParse(authInfo.UserId.ToString(), out Guid userId))
                    {
                        _currentUserId = userId;
                    }
                    
                    if (DateTime.TryParse(authInfo.TokenExpiration.ToString(), out DateTime expiration))
                    {
                        _tokenExpiration = expiration;
                    }
                    else
                    {
                        _tokenExpiration = DateTime.MinValue;
                    }
                    
                    // 检查令牌是否已过期
                    if (IsTokenExpired)
                    {
                        _logger.LogInformation("存储的令牌已过期，清除身份验证状态");
                        Logout();
                    }
                    else
                    {
                        _logger.LogInformation("成功恢复身份验证状态，用户ID: {UserId}", _currentUserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复身份验证状态失败");
                _token = null;
                _currentUserId = null;
                _tokenExpiration = DateTime.MinValue;
            }
        }
    }
}