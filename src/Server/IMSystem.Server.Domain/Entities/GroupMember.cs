using IMSystem.Server.Domain.Common; // For AuditableEntity
using IMSystem.Server.Domain.Enums; // For GroupMemberRole
using IMSystem.Server.Domain.Exceptions; // For DomainException, though ArgumentException is also used
using System;

namespace IMSystem.Server.Domain.Entities
{
    /// <summary>
    /// 代表用户与群组之间的成员关系。
    /// </summary>
    public class GroupMember : AuditableEntity // 继承自 AuditableEntity
    {
        private const int NicknameMaxLength = 50;

        /// <summary>
        /// 所属群组的ID。
        /// </summary>
        public Guid GroupId { get; private set; }

        /// <summary>
        /// 导航属性，指向所属的群组。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual Group Group { get; private set; }
#pragma warning restore CS8618

        /// <summary>
        /// 成员的用户ID。
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// 导航属性，指向成员用户。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual User User { get; private set; }
#pragma warning restore CS8618

        /// <summary>
        /// 成员在此群组中的角色。
        /// </summary>
        public GroupMemberRole Role { get; private set; }

        /// <summary>
        /// 成员在群组中的昵称（可选）。
        /// </summary>
        public string? NicknameInGroup { get; private set; }

        // Id, CreatedAt (代表 JoinedAt), CreatedBy, LastModifiedAt, LastModifiedBy 属性来自 AuditableEntity
        // CreatedAt 将代表加入时间。
        // CreatedBy 可以是添加此成员的用户ID（例如管理员），或者是成员自己的ID（如果用户主动加入）。

        /// <summary>
        /// EF Core 使用的私有构造函数。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private GroupMember() : base() { } // 调用基类构造函数
#pragma warning restore CS8618

        /// <summary>
        /// 创建一个新的群组成员关系实例。
        /// </summary>
        /// <param name="groupId">群组ID。</param>
        /// <param name="userId">用户ID。</param>
        /// <param name="role">成员角色，默认为普通成员。</param>
        /// <param name="nicknameInGroup">在群组中的昵称（可选）。</param>
        /// <param name="actorId">执行此操作的用户ID（可选，例如邀请者或用户自己）。</param>
        public GroupMember(Guid groupId, Guid userId, GroupMemberRole role = GroupMemberRole.Member, string? nicknameInGroup = null, Guid? actorId = null)
        {
            // Id 和 CreatedAt (JoinedAt) 由基类构造函数设置
            if (groupId == Guid.Empty)
                throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            if (actorId.HasValue && actorId.Value == Guid.Empty)
                throw new ArgumentException("Actor ID, if provided, cannot be an empty GUID.", nameof(actorId));
            if (!Enum.IsDefined(typeof(GroupMemberRole), role))
                throw new ArgumentException("Invalid group member role.", nameof(role));

            GroupId = groupId;
            UserId = userId;
            Role = role;
            SetNickname(nicknameInGroup, actorId ?? userId); // Initial nickname set with validation

            CreatedBy = actorId ?? userId; // 如果未指定操作者，则默认为成员自己
            LastModifiedAt = CreatedAt;    // 初始时 LastModifiedAt 等于 CreatedAt
            LastModifiedBy = CreatedBy;

            // 添加领域事件
            this.AddDomainEvent(new Events.Groups.GroupMemberAddedEvent(
                this.Id,
                this.GroupId,
                this.UserId,
                this.Role,
                this.NicknameInGroup,
                this.CreatedBy.Value
            ));
        }

        /// <summary>
        /// 更新成员在群组中的角色。
        /// </summary>
        /// <param name="newRole">新的角色。</param>
        /// <param name="modifierId">执行修改操作的用户ID。</param>
        public void UpdateRole(GroupMemberRole newRole, Guid modifierId)
        {
            if (modifierId == Guid.Empty)
                throw new ArgumentException("Modifier ID cannot be empty.", nameof(modifierId));
            if (!Enum.IsDefined(typeof(GroupMemberRole), newRole))
                throw new ArgumentException("Invalid new group member role.", nameof(newRole));

            // TODO: 添加业务逻辑验证，例如：
            // 1. modifierId 是否有权限更改角色 (例如，必须是群主或管理员)。
            // 2. 不能将群主的角色更改为非群主，除非同时转移群主身份。
            // 3. 一个群组只能有一个群主。
            //    这些复杂的验证通常在应用服务层处理，因为它们可能需要查询其他数据。

            if (Role != newRole)
            {
                GroupMemberRole oldRole = Role;
                Role = newRole;
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = modifierId;
                
                // 添加领域事件 - 使用正确的事件类型和参数
                this.AddDomainEvent(new Events.Groups.GroupMemberRoleUpdatedEvent(
                    this.GroupId,
                    this.Group?.Name ?? "Unknown", // 群组名，可能需要从导航属性获取或通过仓储查询
                    this.UserId,
                    this.User?.Username ?? "Unknown", // 成员用户名，可能需要从导航属性获取或通过仓储查询
                    oldRole,
                    newRole,
                    modifierId,
                    "Unknown" // 操作者用户名，通常需要通过仓储查询
                ));
            }
        }

        private void SetNickname(string? nickname, Guid modifierId)
        {
            if (modifierId == Guid.Empty) // Though modifierId is internal to this method call path
                throw new ArgumentException("Modifier ID cannot be empty when setting nickname.", nameof(modifierId));

            if (nickname != null && nickname.Length > NicknameMaxLength)
                throw new DomainException($"Nickname cannot exceed {NicknameMaxLength} characters.");

            if (NicknameInGroup != nickname)
            {
                NicknameInGroup = nickname;
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = modifierId;
            }
        }

        /// <summary>
        /// 更新成员在群组中的昵称。
        /// </summary>
        /// <param name="newNickname">新的昵称（如果为 null，则清除昵称）。</param>
        /// <param name="modifierId">执行修改操作的用户ID（可以是成员自己或管理员）。</param>
        public void UpdateNickname(string? newNickname, Guid modifierId)
        {
            if (modifierId == Guid.Empty)
                throw new ArgumentException("Modifier ID cannot be empty.", nameof(modifierId));
                
            string? oldNickname = NicknameInGroup;
            SetNickname(newNickname, modifierId);
            
            // 如果昵称确实发生了变化，才触发领域事件
            if (oldNickname != NicknameInGroup)
            {
                // 添加领域事件
                this.AddDomainEvent(new Events.Groups.GroupMemberNicknameChangedEvent(
                    this.Id,
                    this.GroupId,
                    this.UserId,
                    oldNickname,
                    this.NicknameInGroup,
                    modifierId
                ));
            }
        }
    }
}