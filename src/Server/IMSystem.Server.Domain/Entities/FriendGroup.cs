using IMSystem.Server.Domain.Common; // For AuditableEntity
using IMSystem.Server.Domain.Exceptions;
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Domain.Entities
{
    /// <summary>
    /// 代表用户自定义的好友分组。
    /// </summary>
    public class FriendGroup : AuditableEntity // 继承自 AuditableEntity
    {
        private const int NameMinLength = 1;
        private const int NameMaxLength = 50;

        // Id, CreatedAt, CreatedBy (代表 UserId), LastModifiedAt, LastModifiedBy (代表 UserId) 来自 AuditableEntity

        /// <summary>
        /// 好友分组的名称。
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 用于对用户的好友分组进行排序的序号。
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// 指示此分组是否为用户的默认分组（例如，“我的好友”）。
        /// 每个用户通常只有一个默认分组。
        /// </summary>
        public bool IsDefault { get; private set; }

        /// <summary>
        /// 导航属性，指向拥有此好友分组的用户。
        /// CreatedBy 存储用户ID。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual User User { get; private set; } // EF Core 会根据 CreatedBy 关联
#pragma warning restore CS8618


        // 根据文档："User聚合...不直接管理分组下的好友列表"
        // 这意味着 FriendGroup 实体本身不包含一个 Friendships 或 UserFriendGroups 的集合。
        // UserFriendGroup 实体将用于连接 User (好友) 和 FriendGroup。

        /// <summary>
        /// EF Core 使用的私有构造函数。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private FriendGroup() : base() { } // 调用基类构造函数
#pragma warning restore CS8618

        /// <summary>
        /// 创建一个新的好友分组实例。
        /// </summary>
        /// <param name="userId">创建此分组的用户ID。</param>
        /// <param name="name">分组名称。</param>
        /// <param name="order">排序序号，默认为0。</param>
        /// <param name="isDefault">是否为默认分组，默认为 false。</param>
        public FriendGroup(Guid userId, string name, int order = 0, bool isDefault = false)
        {
            // Id 和 CreatedAt 由基类构造函数设置
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID (CreatedBy) cannot be empty.", nameof(userId));

            CreatedBy = userId; // 分组的拥有者即为创建者
            SetName(name);
            SetOrder(order); // 虽然Order目前没有复杂验证，但保持封装性
            IsDefault = isDefault; // IsDefault 的逻辑通常在应用服务层管理，确保唯一性等
            LastModifiedAt = CreatedAt; // 初始时 LastModifiedAt 等于 CreatedAt
            LastModifiedBy = userId;    // 初始修改者为创建者

            // TODO: 考虑触发一个 FriendGroupCreatedDomainEvent
            // AddDomainEvent(new FriendGroupCreatedDomainEvent(this));
        }

        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Friend group name cannot be empty.");
            if (name.Length < NameMinLength || name.Length > NameMaxLength)
                throw new DomainException($"Friend group name must be between {NameMinLength} and {NameMaxLength} characters.");
            Name = name;
        }

        private void SetOrder(int order)
        {
            // 目前对 Order 没有特定验证规则，如果未来有（例如不能为负数），可在此添加
            Order = order;
        }

        /// <summary>
        /// 更新好友分组的详细信息。
        /// 默认分组的 IsDefault 状态不应通过此方法更改。
        /// </summary>
        /// <param name="newName">新的分组名称。</param>
        /// <param name="newOrder">新的排序序号。</param>
        /// <param name="actorId">执行修改操作的用户ID。</param>
        public void UpdateDetails(string newName, int newOrder, Guid actorId)
        {
            if (actorId == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

            // IsDefault 属性不应通过此方法修改。
            // 对默认分组名称的修改限制应在应用服务层处理（例如，不能修改默认分组的名称为非默认名称）。

            // 权限检查 (例如, actorId == this.CreatedBy) 通常在应用服务层进行。
            // if (this.CreatedBy != actorId)
            //     throw new DomainException("Only the owner can update friend group details.");

            bool updated = false;
            if (Name != newName)
            {
                SetName(newName);
                updated = true;
            }
            if (Order != newOrder)
            {
                SetOrder(newOrder);
                updated = true;
            }

            if (updated)
            {
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = actorId;
            }
        }

        /// <summary>
        /// 内部方法，用于系统设置或取消默认状态（例如，在极特殊的数据修复场景）。
        /// 通常不应被常规业务逻辑调用。
        /// </summary>
        internal void SetDefaultStatus(bool isDefault, Guid actorId)
        {
            if (this.IsDefault != isDefault)
            {
                this.IsDefault = isDefault;
                this.LastModifiedAt = DateTimeOffset.UtcNow;
                this.LastModifiedBy = actorId; // 记录执行此敏感操作的ID
            }
        }
    }
}