using IMSystem.Server.Domain.Common; // For AuditableEntity
using IMSystem.Server.Domain.Enums; // For FriendshipStatus
using System;

namespace IMSystem.Server.Domain.Entities
{
    /// <summary>
    /// 代表两个用户之间的好友关系。
    /// </summary>
    public class Friendship : AuditableEntity // 继承自 AuditableEntity
    {
        // Id, CreatedAt (代表请求发送时间), CreatedBy (代表 RequesterId), LastModifiedAt, LastModifiedBy 来自 AuditableEntity

        /// <summary>
        /// 发送好友请求的用户ID (由 CreatedBy 表示)。
        /// </summary>
        public Guid RequesterId => CreatedBy.HasValue ? CreatedBy.Value : throw new InvalidOperationException("Friendship.CreatedBy (RequesterId) cannot be null."); // Should always have a value due to constructor logic.

        /// <summary>
        /// 导航属性，指向发送好友请求的用户。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual User Requester { get; private set; }
#pragma warning restore CS8618

        /// <summary>
        /// 接收好友请求的用户ID。
        /// </summary>
        public Guid AddresseeId { get; private set; }

        /// <summary>
        /// 导航属性，指向接收好友请求的用户。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual User Addressee { get; private set; }
#pragma warning restore CS8618

        /// <summary>
        /// 当前好友关系的状态。
        /// </summary>
        public FriendshipStatus Status { get; private set; }

        /// <summary>
        /// 阻止操作的发起方用户ID（可空）。
        /// </summary>
        public Guid? BlockedById { get; private set; }

        /// <summary>
        /// 阻止操作的时间（可空）。
        /// </summary>
        public DateTimeOffset? BlockedAt { get; private set; }

        /// <summary>
        /// 请求者对接收者的备注 (可选)。
        /// </summary>
        public string? RequesterRemark { get; private set; }

        /// <summary>
        /// 接收者对请求者的备注 (可选)。
        /// </summary>
        public string? AddresseeRemark { get; private set; }

        /// <summary>
        /// The expiration time for a pending friend request.
        /// Null if the request is not pending or has no expiration.
        /// </summary>
        public DateTimeOffset? RequestExpiresAt { get; private set; }

        /// <summary>
        /// 检查好友关系是否已确认（即状态为已接受）
        /// </summary>
        /// <returns>如果状态为已接受，则返回 true；否则返回 false</returns>
        public bool IsConfirmed()
        {
            return Status == FriendshipStatus.Accepted;
        }

        /// <summary>
        /// EF Core 使用的私有构造函数。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private Friendship() : base() { } // 调用基类构造函数
#pragma warning restore CS8618

        /// <summary>
        /// 创建一个新的好友请求实例。
        /// </summary>
        /// <param name="requesterId">发送请求的用户ID。</param>
        /// <param name="addresseeId">接收请求的用户ID。</param>
        public Friendship(Guid requesterId, Guid addresseeId)
        {
            if (requesterId == addresseeId)
            {
                throw new ArgumentException("请求者和接收者不能是同一用户。");
            }

            // Id 和 CreatedAt 由基类构造函数设置
            CreatedBy = requesterId; // 请求者即为创建者
            AddresseeId = addresseeId;
            Status = FriendshipStatus.Pending;
            RequestExpiresAt = DateTimeOffset.UtcNow.AddDays(7); // Default expiration: 7 days
            LastModifiedAt = CreatedAt; // 初始时 LastModifiedAt 等于 CreatedAt
            LastModifiedBy = requesterId; // 初始修改者为请求者

            // TODO: 考虑触发一个 FriendshipRequestedDomainEvent
            // AddDomainEvent(new FriendshipRequestedDomainEvent(this));
        }

        /// <summary>
        /// 接受好友请求。
        /// </summary>
        /// <param name="actorId">执行此操作的用户ID（即 AddresseeId）。</param>
        public void AcceptRequest(Guid actorId)
        {
            if (Status == FriendshipStatus.Pending)
            {
                if (actorId != AddresseeId)
                {
                    // 理论上只有接收者可以接受请求，或者系统管理员（如果业务允许）
                    throw new InvalidOperationException("只有接收者才能接受好友请求。");
                }
                Status = FriendshipStatus.Accepted;
                RequestExpiresAt = null; // Clear expiration on acceptance
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = actorId;
                
                // 添加领域事件
                this.AddDomainEvent(new Events.Friends.FriendRequestAcceptedEvent(
                    this.Id,
                    this.RequesterId,
                    this.AddresseeId,
                    this.Addressee?.Username ?? "Unknown",
                    this.Addressee?.Profile?.Nickname,
                    this.Addressee?.Profile?.AvatarUrl
                ));
            }
            // else: 可以抛出异常或记录错误，表明请求已被处理或状态不正确
        }

        /// <summary>
        /// 拒绝好友请求。
        /// </summary>
        /// <param name="actorId">执行此操作的用户ID（即 AddresseeId）。</param>
        public void DeclineRequest(Guid actorId)
        {
            if (Status == FriendshipStatus.Pending)
            {
                if (actorId != AddresseeId)
                {
                    throw new InvalidOperationException("只有接收者才能拒绝好友请求。");
                }
                Status = FriendshipStatus.Declined;
                RequestExpiresAt = null; // Clear expiration on decline
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = actorId;
                
                // 添加领域事件
                this.AddDomainEvent(new Events.Friends.FriendRequestDeclinedEvent(
                    this.Id,
                    this.RequesterId,
                    this.AddresseeId,
                    this.Addressee?.Username ?? "Unknown" // 添加declinerUsername参数
                ));
            }
        }

        /// <summary>
        /// 阻止用户（建立或更新阻止状态）。
        /// </summary>
        /// <param name="blockerId">执行阻止操作的用户ID (可以是 RequesterId 或 AddresseeId)。</param>
        public void Block(Guid blockerId)
        {
            // 业务逻辑：谁执行了阻止操作，谁就是 LastModifiedBy
            // 阻止操作可能由请求者或接收者发起。
            // 确保 blockerId 是此好友关系中的一方。
            if (blockerId != RequesterId && blockerId != AddresseeId)
            {
                throw new InvalidOperationException("执行阻止操作的用户必须是好友关系的一方。");
            }

            if (Status != FriendshipStatus.Blocked)
            {
                Status = FriendshipStatus.Blocked;
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = blockerId;
                BlockedById = blockerId;
                BlockedAt = DateTimeOffset.UtcNow;

                // 添加领域事件
                this.AddDomainEvent(new Events.Friends.FriendBlockedEvent(
                    blockerId,
                    blockerId == RequesterId ? AddresseeId : RequesterId,
                    this.Id
                ));
            }
        }

        /// <summary>
        /// 解除阻止用户。
        /// </summary>
        /// <param name="unblockerId">执行解除阻止操作的用户ID。</param>
        public void Unblock(Guid unblockerId)
        {
            // 业务逻辑：谁执行了解除阻止操作
            if (unblockerId != RequesterId && unblockerId != AddresseeId)
            {
                throw new InvalidOperationException("执行解除阻止操作的用户必须是好友关系的一方。");
            }

            if (Status == FriendshipStatus.Blocked)
            {
                // 确保只有最初执行阻止的用户才能解除阻止
                if (BlockedById != unblockerId)
                {
                    throw new InvalidOperationException("只有最初执行阻止的用户才能解除阻止。");
                }

                // Business Decision: Upon unblocking, the relationship status defaults to 'Accepted'.
                // This allows users to immediately re-engage.
                // Future enhancements might involve restoring to the pre-blocked status or another state,
                // which would require more complex logic (e.g., storing previous status).
                // For current requirements, 'Accepted' is the defined behavior post-unblock.
                Status = FriendshipStatus.Accepted; // 或者恢复到阻止前的状态，如果需要更复杂的逻辑
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = unblockerId;
                BlockedById = null;
                BlockedAt = null;

                // 添加领域事件
                this.AddDomainEvent(new Events.Friends.FriendUnblockedEvent(
                    unblockerId,
                    unblockerId == RequesterId ? AddresseeId : RequesterId,
                    this.Id
                ));
            }
        }

        /// <summary>
        /// 更新好友备注。
        /// </summary>
        /// <param name="actorId">执行备注操作的用户ID (必须是关系中的一方)。</param>
        /// <param name="newRemark">新的备注内容。</param>
        public void UpdateRemark(Guid actorId, string? newRemark)
        {
            if (actorId == RequesterId)
            {
                // 请求者在修改其对接收者的备注
                if (RequesterRemark != newRemark)
                {
                    RequesterRemark = newRemark;
                    LastModifiedAt = DateTimeOffset.UtcNow;
                    LastModifiedBy = actorId;
                    
                    // 添加领域事件
                    this.AddDomainEvent(new Events.Friends.FriendRemarkUpdatedEvent(
                        actorId,
                        AddresseeId,
                        this.Id,
                        newRemark,
                        true // requester对addressee的备注
                    ));
                }
            }
            else if (actorId == AddresseeId)
            {
                // 接收者在修改其对请求者的备注
                if (AddresseeRemark != newRemark)
                {
                    AddresseeRemark = newRemark;
                    LastModifiedAt = DateTimeOffset.UtcNow;
                    LastModifiedBy = actorId;
                    
                    // 添加领域事件
                    this.AddDomainEvent(new Events.Friends.FriendRemarkUpdatedEvent(
                        actorId,
                        RequesterId,
                        this.Id,
                        newRemark,
                        false // addressee对requester的备注
                    ));
                }
            }
            else
            {
                throw new InvalidOperationException("执行备注操作的用户必须是好友关系的一方。");
            }
        }
    }
}