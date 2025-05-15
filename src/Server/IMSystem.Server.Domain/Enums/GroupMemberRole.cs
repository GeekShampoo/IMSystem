namespace IMSystem.Server.Domain.Enums
{
    /// <summary>
    /// 定义群组成员的角色。
    /// </summary>
    public enum GroupMemberRole
    {
        /// <summary>
        /// 普通成员。
        /// </summary>
        Member,
        /// <summary>
        /// 群组管理员。
        /// </summary>
        Admin,
        /// <summary>
        /// 群主。
        /// </summary>
        Owner
    }
}