namespace IMSystem.Server.Core.Settings
{
    /// <summary>
    /// 用户相关业务参数配置。
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// 邮件验证令牌有效期（天数）。
        /// </summary>
        public int EmailVerificationTokenExpiryDays { get; set; }
    }
}