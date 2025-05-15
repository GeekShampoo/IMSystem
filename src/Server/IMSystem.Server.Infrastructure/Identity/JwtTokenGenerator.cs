using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IMSystem.Server.Infrastructure.Identity
{
    /// <summary>
    /// JWT (JSON Web Token) 的配置设置。
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// 获取或设置用于签名和验证令牌的密钥。
        /// </summary>
        public string Secret { get; set; }
        /// <summary>
        /// 获取或设置令牌的颁发者。
        /// </summary>
        public string Issuer { get; set; }
        /// <summary>
        /// 获取或设置令牌的受众。
        /// </summary>
        public string Audience { get; set; }
        /// <summary>
        /// 获取或设置令牌的过期时间（分钟）。
        /// </summary>
        public int ExpiryMinutes { get; set; } // Renamed for clarity to match usage
    }

    /// <summary>
    /// JWT 令牌生成器的实现。
    /// </summary>
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        /// <summary>
        /// 初始化 <see cref="JwtTokenGenerator"/> 类的新实例。
        /// </summary>
        /// <param name="jwtOptions">JWT 配置选项。</param>
        /// <exception cref="ArgumentNullException">如果 jwtOptions 或其关键属性为空。</exception>
        /// <exception cref="ArgumentOutOfRangeException">如果 ExpiryMinutes 小于或等于零。</exception>
        public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value ?? throw new ArgumentNullException(nameof(jwtOptions), "JWT settings cannot be null.");
            if (string.IsNullOrEmpty(_jwtSettings.Secret)) throw new ArgumentNullException(nameof(_jwtSettings.Secret), "JWT Secret cannot be null or empty.");
            if (string.IsNullOrEmpty(_jwtSettings.Issuer)) throw new ArgumentNullException(nameof(_jwtSettings.Issuer), "JWT Issuer cannot be null or empty.");
            if (string.IsNullOrEmpty(_jwtSettings.Audience)) throw new ArgumentNullException(nameof(_jwtSettings.Audience), "JWT Audience cannot be null or empty.");
            if (_jwtSettings.ExpiryMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(_jwtSettings.ExpiryMinutes), "JWT ExpiryMinutes must be greater than zero.");
        }

        /// <inheritdoc/>
        public (string Token, DateTime ExpiresAt) GenerateToken(User user, IEnumerable<string>? roles = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Subject (user ID)
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username), // Unique name (username)
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ASP.NET Core Identity standard for User ID
                new Claim(ClaimTypes.Name, user.Username) // ASP.NET Core Identity standard for Username
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), tokenDescriptor.Expires.Value);
        }
    }
}