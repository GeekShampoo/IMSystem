using IMSystem.Server.Domain.Common; // For AuditableEntity
using IMSystem.Server.Domain.Exceptions; // 虽然这里可能主要用 ArgumentException，但保持一致性
using System;

namespace IMSystem.Server.Domain.Entities
{
    /// <summary>
    /// 代表用户好友与用户自定义好友分组之间的关联。
    /// 此实体用于将一个用户的好友（通过 Friendship 实体确定）分配到该用户的一个好友分组中。
    /// </summary>
    public class UserFriendGroup : AuditableEntity // 继承自 AuditableEntity
    {

        /// <summary>
        /// 执行分组操作的用户ID，也是好友分组的拥有者。
        /// 由 CreatedBy 属性表示。
        /// </summary>
        public Guid UserId => CreatedBy.HasValue ? CreatedBy.Value : throw new InvalidOperationException("UserFriendGroup.CreatedBy cannot be null.");

        /// <summary>
        /// 导航属性，指向执行分组操作的用户。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual User User { get; private set; } // EF Core 会根据 CreatedBy (UserId) 关联
#pragma warning restore CS8618

        /// <summary>
        /// 关联的好友关系ID。通过此ID可以找到具体的 Friendship 记录，进而确定好友是谁。
        /// </summary>
        public Guid FriendshipId { get; private set; }

        /// <summary>
        /// 导航属性，指向关联的 Friendship 记录。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual Friendship Friendship { get; private set; }
#pragma warning restore CS8618

        /// <summary>
        /// 好友被分配到的好友分组ID。
        /// </summary>
        public Guid FriendGroupId { get; private set; }

        /// <summary>
        /// 导航属性，指向好友被分配到的好友分组。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual FriendGroup FriendGroup { get; private set; }
#pragma warning restore CS8618


        /// <summary>
        /// EF Core 使用的私有构造函数。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private UserFriendGroup() : base() { } // 调用基类构造函数
#pragma warning restore CS8618

        /// <summary>
        /// 创建一个新的用户好友与分组的关联实例。
        /// </summary>
        /// <param name="userId">执行分组操作的用户ID（即分组的拥有者）。</param>
        /// <param name="friendshipId">要分组的好友关系ID。</param>
        /// <param name="friendGroupId">要将好友分配到的分组ID。</param>
        public UserFriendGroup(Guid userId, Guid friendshipId, Guid friendGroupId)
        {
            // Id 和 CreatedAt 由基类构造函数设置
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID (CreatedBy) cannot be empty.", nameof(userId));
            if (friendshipId == Guid.Empty)
                throw new ArgumentException("Friendship ID cannot be empty.", nameof(friendshipId));
            if (friendGroupId == Guid.Empty)
                throw new ArgumentException("FriendGroup ID cannot be empty.", nameof(friendGroupId));

            CreatedBy = userId; // 执行分组操作的用户
            FriendshipId = friendshipId;
            FriendGroupId = friendGroupId;

            LastModifiedAt = CreatedAt; // 初始时 LastModifiedAt 等于 CreatedAt
            LastModifiedBy = userId;    // 初始修改者为执行分组的用户


        }

        /// <summary>
        /// 将好友移动到另一个分组。
        /// </summary>
        /// <param name="newFriendGroupId">新的好友分组ID。</param>
        /// <param name="actorId">执行此操作的用户ID（即分组的拥有者）。</param>
        public void MoveToGroup(Guid newFriendGroupId, Guid actorId)
        {
            if (actorId == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            if (newFriendGroupId == Guid.Empty)
                throw new ArgumentException("New FriendGroup ID cannot be empty.", nameof(newFriendGroupId));

            if (actorId != UserId) // UserId is derived from CreatedBy
            {
                throw new DomainException("只有分组的拥有者才能移动好友到其他分组。");
            }


            if (FriendGroupId != newFriendGroupId)
            {
                FriendGroupId = newFriendGroupId;
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = actorId;
            }
        }

    }
}