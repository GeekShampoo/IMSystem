using IMSystem.Server.Domain.Common; // For AuditableEntity
using IMSystem.Server.Domain.Exceptions; // For DomainException
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Domain.Entities
{
    /// <summary>
    /// 代表一个用户群组。
    /// </summary>
    public class Group : AuditableEntity // 继承自 AuditableEntity
    {
        private const int NameMinLength = 1;
        private const int NameMaxLength = 100;
        private const int DescriptionMaxLength = 500; // Example length
        private const int AvatarUrlMaxLength = 2048; // Example length


        /// <summary>
        /// 群组的名称。
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 群组的描述信息（可选）。
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 群组头像的 URL（可选）。
        /// </summary>
        public string? AvatarUrl { get; private set; }

        /// <summary>
        /// 当前群主的的用户ID。
        /// </summary>
        public Guid OwnerId { get; private set; }

        /// <summary>
        /// 导航属性，指向当前群主用户。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual User Owner { get; private set; }
#pragma warning restore CS8618

        // Id, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy 属性来自 AuditableEntity
        // CreatedBy 将代表群组的初始创建者。

        /// <summary>
        /// 导航属性，包含此群组的所有成员。
        /// </summary>
        public virtual ICollection<GroupMember> Members { get; private set; } = new List<GroupMember>();

        /// <summary>
        /// The group's current announcement.
        /// </summary>
        public string? Announcement { get; private set; }

        /// <summary>
        /// The timestamp when the current announcement was set.
        /// </summary>
        public DateTimeOffset? AnnouncementSetAt { get; private set; }

        /// <summary>
        /// The ID of the user who set the current announcement.
        /// </summary>
        public Guid? AnnouncementSetByUserId { get; private set; }
        // public virtual User? AnnouncementSetByUser { get; private set; } // Optional navigation

        /// <summary>
        /// EF Core 使用的私有构造函数。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private Group() : base() { } // 调用基类构造函数
#pragma warning restore CS8618

        /// <summary>
        /// 创建一个新的群组实例。
        /// </summary>
        /// <param name="name">群组名称。</param>
        /// <param name="creatorId">创建者（初始群主）的用户ID。</param>
        /// <param name="description">群组描述（可选）。</param>
        /// <param name="avatarUrl">群组头像 URL（可选）。</param>
        public Group(string name, Guid creatorId, string? description = null, string? avatarUrl = null)
        {
            // Id 和 CreatedAt 由基类构造函数设置
            if (creatorId == Guid.Empty)
                throw new ArgumentException("Creator ID cannot be empty.", nameof(creatorId));

            SetName(name); // Use private setter with validation
            SetDescription(description);
            SetAvatarUrl(avatarUrl);

            CreatedBy = creatorId; // 设置创建者
            OwnerId = creatorId;   // 初始群主即为创建者
            LastModifiedAt = CreatedAt; // 初始时 LastModifiedAt 等于 CreatedAt
            LastModifiedBy = creatorId;

            // 添加领域事件 - 修复参数
            this.AddDomainEvent(new Events.Groups.GroupCreatedEvent(
                this.Id,
                this.Name,
                creatorId
            ));

            // 注意：初始成员（群主）的添加通常在应用服务层处理，
            // 因为它可能涉及到创建 GroupMember 实体并保存。
            // 例如：
            // var ownerMember = new GroupMember(this.Id, creatorId, GroupMemberRole.Owner);
            // Members.Add(ownerMember); // 这只是添加到集合，实际保存需通过DbContext
        }

        /// <summary>
        /// 更新群组的详细信息。
        /// </summary>
        /// <param name="name">新的群组名称。</param>
        /// <param name="description">新的群组描述（如果为 null，则不更新）。</param>
        /// <param name="avatarUrl">新的群组头像 URL（如果为 null，则不更新）。</param>
        /// <param name="modifierId">执行修改操作的用户ID。</param>
        public void UpdateDetails(string name, string? description, string? avatarUrl, Guid modifierId)
        {
            if (modifierId == Guid.Empty)
                throw new ArgumentException("Modifier ID cannot be empty.", nameof(modifierId));

            // TODO: 添加权限校验，例如只有群主或管理员才能修改 (通常在应用服务层)
            bool updated = false;
            string oldName = Name;
            string? oldDescription = Description;
            string? oldAvatarUrl = AvatarUrl;
            
            if (Name != name)
            {
                SetName(name);
                updated = true;
            }
            if (description != null && Description != description) // Allow clearing description by passing ""
            {
                SetDescription(description);
                updated = true;
            }
            else if (description == null && Description != null) // If explicitly passing null to clear
            {
                 SetDescription(null);
                 updated = true;
            }

            if (avatarUrl != null && AvatarUrl != avatarUrl) // Allow clearing avatar by passing ""
            {
                SetAvatarUrl(avatarUrl);
                updated = true;
            }
             else if (avatarUrl == null && AvatarUrl != null) // If explicitly passing null to clear
            {
                 SetAvatarUrl(null);
                 updated = true;
            }

            if (updated)
            {
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = modifierId;
                
                // 添加领域事件 - 修复参数顺序
                this.AddDomainEvent(new Events.Groups.GroupDetailsUpdatedEvent(
                    this.Id,
                    modifierId,
                    this.Name,
                    oldName,
                    this.Description,
                    oldDescription,
                    this.AvatarUrl,
                    oldAvatarUrl
                ));
            }
        }

        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Group name cannot be empty.");
            if (name.Length < NameMinLength || name.Length > NameMaxLength)
                throw new DomainException($"Group name must be between {NameMinLength} and {NameMaxLength} characters.");
            Name = name;
        }

        private void SetDescription(string? description)
        {
            if (description != null && description.Length > DescriptionMaxLength)
                throw new DomainException($"Group description cannot exceed {DescriptionMaxLength} characters.");
            Description = description;
        }

        private void SetAvatarUrl(string? avatarUrl)
        {
            if (avatarUrl != null)
            {
                 if (string.IsNullOrWhiteSpace(avatarUrl)) // if provided, it cannot be just whitespace
                 {
                    // Allow empty string to clear, but not just whitespace
                    if(avatarUrl.Length > 0) throw new DomainException("Avatar URL cannot be empty or whitespace if provided with content.");
                    AvatarUrl = string.Empty; // Treat as clearing
                 }
                 else if (avatarUrl.Length > AvatarUrlMaxLength)
                 {
                    throw new DomainException($"Avatar URL cannot exceed {AvatarUrlMaxLength} characters.");
                 }
                 // Optional: Add URL format validation
                 // else if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
                 //    throw new DomainException("Invalid avatar URL format.");
                 else
                 {
                    AvatarUrl = avatarUrl;
                 }
            }
            else
            {
                AvatarUrl = null; // Explicitly set to null if null is passed
            }
        }


        /// <summary>
        /// 转移群主权限。
        /// </summary>
        /// <param name="newOwnerId">新群主的用户ID。</param>
        /// <param name="modifierId">执行修改操作的用户ID（通常是原群主或管理员）。</param>
        public void TransferOwnership(Guid newOwnerId, Guid modifierId)
        {
            if (modifierId == Guid.Empty)
                throw new ArgumentException("Modifier ID cannot be empty.", nameof(modifierId));
            if (newOwnerId == Guid.Empty)
                throw new ArgumentException("New owner ID cannot be empty.", nameof(newOwnerId));

            // TODO: 添加业务逻辑验证：
            // 1. modifierId 是否有权限执行此操作 (例如，必须是当前群主或系统管理员)。
            // 2. newOwnerId 是否是群组的有效成员。
            //    这些复杂的验证通常在应用服务层处理，因为它们可能需要查询其他数据。

            if (OwnerId == newOwnerId)
            {
                // 或者抛出异常，或者静默处理
                return; // No change needed
            }

            Guid oldOwnerId = OwnerId;
            OwnerId = newOwnerId;
            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = modifierId;

            // 添加领域事件 - 修复参数数量
            // 注意：某些参数如用户名需要从数据库获取，这里使用导航属性或占位符
            this.AddDomainEvent(new Events.Groups.GroupOwnershipTransferredEvent(
                this.Id, 
                this.Name,
                oldOwnerId, 
                Owner?.Username ?? "Unknown", // 原群主用户名，可能需要通过仓储获取
                newOwnerId,
                "Unknown", // 新群主用户名，通常需要通过仓储获取
                modifierId
            ));

            // 注意：更新原群主和新群主在 Members 集合中的角色通常在应用服务层处理，
            // 因为这涉及到修改 GroupMember 实体的属性并保存。
        }

        /// <summary>
        /// Sets or clears the group announcement.
        /// </summary>
        /// <param name="announcement">The new announcement text. Null or empty to clear.</param>
        /// <param name="actorId">The ID of the user setting the announcement (must be owner or admin).</param>
        public void SetAnnouncement(string? announcement, Guid actorId)
        {
            if (actorId == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty when setting announcement.", nameof(actorId));

            // Basic validation for announcement length if needed (e.g., max 1000 chars)
            const int announcementMaxLength = 1000;
            if (announcement != null && announcement.Length > announcementMaxLength)
                throw new DomainException($"Group announcement cannot exceed {announcementMaxLength} characters.");

            string? newAnnouncement = string.IsNullOrWhiteSpace(announcement) ? null : announcement.Trim();

            if (Announcement != newAnnouncement)
            {
                Announcement = newAnnouncement;
                AnnouncementSetAt = newAnnouncement != null ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
                AnnouncementSetByUserId = newAnnouncement != null ? actorId : (Guid?)null;
                
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = actorId;
                
                // 添加领域事件 - 使用正确的事件类型
                this.AddDomainEvent(new Events.Groups.GroupAnnouncementSetEvent(
                    this.Id,
                    this.Name,
                    newAnnouncement,
                    actorId,
                    Owner?.Username ?? "Unknown", // 如果无法获取，使用占位符
                    AnnouncementSetAt
                ));
            }
        }

        // 添加和移除成员的逻辑通常通过 GroupMember 实体和应用层服务来管理，
        // 以便处理更复杂的业务规则（如邀请、审批、权限检查等）。
        // Group 实体本身可以提供一些辅助方法或验证，但核心的持久化操作在应用服务层通过仓储进行。
    }
}