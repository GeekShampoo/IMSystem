using IMSystem.Server.Domain.Common; // For AuditableEntity
using IMSystem.Server.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IMSystem.Server.Domain.Entities
{
    /// <summary>
    /// 代表系统中的用户。
    /// </summary>
    public class User : AuditableEntity // 继承自 AuditableEntity
    {
        private const int UsernameMinLength = 3;
        private const int UsernameMaxLength = 50;
        private const int EmailMaxLength = 255;
        // NicknameMaxLength and ProfilePictureUrlMaxLength will be moved to UserProfile
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        /// <summary>
        /// 用户名，唯一。
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// 存储哈希后的密码。
        /// </summary>
        public string PasswordHash { get; private set; }

        /// 用户的电子邮件地址（可选）。
        /// </summary>
        public string? Email { get; private set; }

        // Nickname and ProfilePictureUrl are moved to UserProfile entity

        /// <summary>
        /// 用户当前的在线状态。
        /// 注意：此状态可能更多地由实时服务（如 PresenceHub）管理，
        /// 此处持久化可能用于表示最后已知状态或默认状态。
        /// </summary>
        public bool IsOnline { get; private set; }

        /// <summary>
        /// 用户的自定义状态消息（例如：“忙碌”，“会议中”）。
        /// </summary>
        public string? CustomStatus { get; private set; }

        /// 用户最后一次被检测为在线的时间。
        /// 主要在用户从在线变为离线时更新。
        /// </summary>
        public DateTimeOffset? LastSeenAt { get; private set; }

        /// <summary>
        /// Navigation property to the user's profile.
        /// </summary>
        public virtual UserProfile? Profile { get; private set; }

        /// <summary>
        /// Indicates if the user's email address has been verified.
        /// </summary>
        public bool IsEmailVerified { get; private set; }

        /// <summary>
        /// Stores the token sent to the user for email verification.
        /// </summary>
        public string? EmailVerificationToken { get; private set; }

        /// <summary>
        /// The expiration date and time for the email verification token.
        /// </summary>
        public DateTimeOffset? EmailVerificationTokenExpiresAt { get; private set; }

        /// <summary>
        /// Indicates if the user account has been deactivated.
        /// </summary>
        public bool IsDeactivated { get; private set; }

        /// <summary>
        /// The date and time when the account was deactivated.
        /// </summary>
        public DateTimeOffset? DeactivatedAt { get; private set; }

        /// <summary>
        /// The timestamp of the user's last successful login.
        /// </summary>
        public DateTimeOffset? LastLoginAt { get; private set; }

        /// <summary>
        /// The IP address from which the user last logged in.
        /// </summary>
        public string? LastLoginIp { get; private set; }


        // Id, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy 属性来自 AuditableEntity

        /// <summary>
        /// EF Core 使用的私有构造函数。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private User() : base() { } // 调用基类构造函数
#pragma warning restore CS8618

        /// <summary>
        /// 创建一个新的用户实例。
        /// </summary>
        /// <param name="username">用户名。</param>
        /// <param name="passwordHash">哈希后的密码。</param>
        /// <param name="email">电子邮件地址（可选）。</param>
        /// <param name="auditCreatorIdOverride">用于审计的创建者ID覆盖。如果为null（例如自注册），则使用用户自身的ID。</param>
        public User(
            string username,
            string passwordHash,
            string? email = null,
            Guid? auditCreatorIdOverride = null)
        {
            // Id and CreatedAt are set by AuditableEntity and BaseEntity constructors (this.Id is available here)

            SetUsername(username);
            SetPasswordHash(passwordHash);
            SetEmail(email);
            // Nickname and ProfilePictureUrl are handled by UserProfile

            // Validate auditCreatorIdOverride if provided
            if (auditCreatorIdOverride.HasValue && auditCreatorIdOverride.Value == Guid.Empty)
                throw new ArgumentException("If provided, auditCreatorIdOverride cannot be an empty Guid.", nameof(auditCreatorIdOverride));

            CreatedBy = auditCreatorIdOverride ?? this.Id; // For self-registration, auditCreatorIdOverride is null, so CreatedBy becomes this.Id
            IsOnline = false; // Default to offline on creation
            CustomStatus = null;
            LastSeenAt = null; // Initially null
            IsEmailVerified = false; // Default to not verified
            EmailVerificationToken = null;
            EmailVerificationTokenExpiresAt = null;
            IsDeactivated = false; // Default to active
            DeactivatedAt = null;
            LastLoginAt = null;
            LastLoginIp = null;
            LastModifiedAt = this.CreatedAt; // 创建时 LastModifiedAt 等于 CreatedAt
            LastModifiedBy = auditCreatorIdOverride ?? this.Id; // Initial modifier is the creator (or self)
        }

        private void SetUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new DomainException("Username cannot be empty.");
            if (username.Length < UsernameMinLength || username.Length > UsernameMaxLength)
                throw new DomainException($"Username must be between {UsernameMinLength} and {UsernameMaxLength} characters.");
            // 用户名唯一性检查应在应用服务层或仓储层进行，涉及数据库查询
            Username = username;
        }

        private void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("Password hash cannot be empty.");
            // 实际项目中可能还会校验哈希的格式或强度，但这里仅做非空检查
            PasswordHash = passwordHash;
        }

        private void SetEmail(string? email)
        {
            if (email != null)
            {
                if (string.IsNullOrWhiteSpace(email))
                    throw new DomainException("Email cannot be empty if provided.");
                if (email.Length > EmailMaxLength)
                    throw new DomainException($"Email cannot exceed {EmailMaxLength} characters.");
                if (!EmailRegex.IsMatch(email))
                    throw new DomainException("Invalid email format.");
            }
            Email = email;
        }

        // SetNickname and SetProfilePictureUrl methods are removed as these properties are now in UserProfile.
        // The UpdateProfile method is also removed. Profile updates will be handled via UserProfile entity.

        /// <summary>
        /// 更改用户的密码。
        /// </summary>
        /// <param name="newPasswordHash">新的哈希密码。</param>
        /// <param name="modifierId">执行修改操作的用户ID（可选）。</param>
        public void ChangePassword(string newPasswordHash, Guid? modifierId = null)
        {
            SetPasswordHash(newPasswordHash);
            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = modifierId;
        }

        /// <summary>
        /// 更新用户的在线状态和自定义状态。
        /// </summary>
        /// <param name="isOnline">新的在线状态。</param>
        /// <param name="customStatus">新的自定义状态 (如果为 null，则不更新；如果为空字符串，则清除状态)。</param>
        /// <param name="modifierId">执行修改操作的用户ID（可选）。</param>
        public void UpdatePresence(bool isOnline, string? customStatus, Guid? modifierId = null)
        {
            bool updated = false;
            if (IsOnline != isOnline)
            {
                IsOnline = isOnline;
                if (!IsOnline) // If user is going offline
                {
                    LastSeenAt = DateTimeOffset.UtcNow;
                }
                updated = true;
            }

            // Update customStatus if the new value is different from the current one.
            // This handles setting a new status, changing an existing status, or clearing the status (by passing null).
            if (CustomStatus != customStatus)
            {
                CustomStatus = customStatus; // Allows customStatus to be null to clear it
                updated = true;
            }

            if (updated)
            {
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = modifierId;
            }
        }

        /// <summary>
        /// Generates a new email verification token and sets its expiration.
        /// </summary>
        /// <param name="tokenLifetime">The lifetime of the token.</param>
        public void GenerateEmailVerificationToken(TimeSpan tokenLifetime)
        {
            // A simple token generation strategy. For production, consider more robust, cryptographically secure random strings.
            EmailVerificationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("+", "").Replace("/", "");
            EmailVerificationTokenExpiresAt = DateTimeOffset.UtcNow.Add(tokenLifetime);
            IsEmailVerified = false; // Ensure it's marked as unverified when a new token is generated
            // LastModifiedAt and LastModifiedBy could be updated here if needed,
            // but typically generating a token isn't a "user modification" in the same sense.
            // However, if this action is user-initiated (e.g., "resend verification email"), then update audit trails.
        }

        /// <summary>
        /// Verifies the user's email using the provided token.
        /// </summary>
        /// <param name="token">The verification token submitted by the user.</param>
        /// <returns>True if verification is successful, false otherwise.</returns>
        public bool VerifyEmail(string token)
        {
            if (IsEmailVerified) return true; // Already verified

            if (string.IsNullOrWhiteSpace(token) ||
                EmailVerificationToken == null ||
                EmailVerificationTokenExpiresAt == null ||
                EmailVerificationToken != token ||
                DateTimeOffset.UtcNow > EmailVerificationTokenExpiresAt)
            {
                // Optionally clear the token if it's invalid or expired to prevent reuse for error messages
                // EmailVerificationToken = null;
                // EmailVerificationTokenExpiresAt = null;
                return false; // Token is invalid, missing, or expired
            }

            IsEmailVerified = true;
            EmailVerificationToken = null; // Clear the token once used
            EmailVerificationTokenExpiresAt = null;
            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = this.Id; // System or user action, here assuming user action via token
            return true;
        }

        /// <summary>
        /// Updates the user's email address and resets verification status.
        /// A new verification token should be generated and sent.
        /// </summary>
        /// <param name="newEmail">The new email address.</param>
        /// <param name="modifierId">The ID of the user/system performing the modification.</param>
        public void UpdateEmail(string newEmail, Guid modifierId)
        {
            SetEmail(newEmail); // Validates and sets the new email
            IsEmailVerified = false;
            EmailVerificationToken = null;
            EmailVerificationTokenExpiresAt = null;
            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = modifierId;
            // Caller should then call GenerateEmailVerificationToken() and trigger email sending.
        }

        /// <summary>
        /// Deactivates the user account.
        /// </summary>
        /// <param name="modifierId">The ID of the user or system performing the deactivation.</param>
        public void Deactivate(Guid modifierId)
        {
            if (!IsDeactivated)
            {
                IsDeactivated = true;
                DeactivatedAt = DateTimeOffset.UtcNow;
                IsOnline = false; // Force offline status
                // Consider other actions: anonymize data, clear tokens, etc.
                // For now, just marking as deactivated and offline.
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = modifierId;
                // TODO: Consider raising a UserDeactivatedDomainEvent
            }
        }

        /// <summary>
        /// Updates the user's last login information.
        /// </summary>
        /// <param name="ipAddress">The IP address from which the user logged in.</param>
        public void UpdateLoginInfo(string? ipAddress)
        {
            LastLoginAt = DateTimeOffset.UtcNow;
            LastLoginIp = ipAddress; // Consider validating or truncating IP address string
            // This is an update, so LastModifiedAt should also be updated.
            // The modifier is the user themselves logging in.
            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = this.Id;
        }
    }
}