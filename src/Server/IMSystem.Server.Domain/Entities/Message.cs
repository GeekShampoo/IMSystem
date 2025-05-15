using IMSystem.Server.Domain.Common; // For AuditableEntity
using IMSystem.Server.Domain.Enums; // For MessageType, MessageRecipientType
using IMSystem.Server.Domain.Events;
using IMSystem.Server.Domain.Exceptions;
using System;
using IMSystem.Server.Domain.Events.Messages;

namespace IMSystem.Server.Domain.Entities
{
    /// <summary>
    /// 代表系统中的一条消息。
    /// </summary>
    public class Message : AuditableEntity // 继承自 AuditableEntity
    {
        private const int ContentMaxLength = 5000;

        /// <summary>
        /// Gets the unique identifier of the message. This is an alias for the Id property from BaseEntity.
        /// </summary>
        public Guid MessageId => Id;

        /// <summary>
        /// Gets the ID of the user who sent the message. This is an alias for the CreatedBy property from AuditableEntity.
        /// </summary>
        public Guid? SenderId => CreatedBy; // CreatedBy is Guid? in AuditableEntity

        /// <summary>
        /// Gets the timestamp when the message was sent. This is an alias for the CreatedAt property from AuditableEntity.
        /// </summary>
        public DateTimeOffset SentAt => CreatedAt;

        /// <summary>
        /// 消息序列号（顺序同步与补拉用）。
        /// </summary>
        public long SequenceNumber { get; private set; }

        /// <summary>
        /// 导航属性，指向发送者用户。
        /// CreatedBy 存储发送者ID。
        /// </summary>
        public virtual User? Sender { get; private set; } // EF Core 会根据 CreatedBy 关联

        /// <summary>
        /// 接收者ID (可以是用户ID或群组ID)。
        /// </summary>
        public Guid RecipientId { get; private set; }

        /// <summary>
        /// 接收者类型。
        /// </summary>
        public MessageRecipientType RecipientType { get; private set; }

        /// <summary>
        /// 如果 RecipientType 是 User，则此导航属性指向接收用户。
        /// </summary>
        public virtual User? RecipientUser { get; private set; }

        /// <summary>
        /// 如果 RecipientType 是 Group，则此导航属性指向接收群组。
        /// </summary>
        public virtual Group? RecipientGroup { get; private set; }

        /// <summary>
        /// 消息内容。
        /// 对于文本类型，这是纯文本。
        /// 对于图片、文件等类型，这可以是文件的URL、元数据JSON或其他标识符。
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// 消息的类型。
        /// </summary>
        public MessageType Type { get; private set; }

        /// <summary>
        /// 消息送达时间（UTC，可选）。
        /// </summary>
        public DateTimeOffset? DeliveredAt { get; private set; }

        /// <summary>
        /// 此消息回复的上一条消息的ID（可选）。
        /// </summary>
        public Guid? ReplyToMessageId { get; private set; }

        /// <summary>
        /// 导航属性，指向被回复的消息（可选）。
        /// </summary>
        public virtual Message? ReplyToMessage { get; private set; }

        /// <summary>
        /// Indicates if the message has been recalled.
        /// </summary>
        public bool IsRecalled { get; private set; }

        /// <summary>
        /// The timestamp when the message was recalled. Null if not recalled.
        /// </summary>
        public DateTimeOffset? RecalledAt { get; private set; }

        /// <summary>
        /// Optional client-generated ID for this message, used for message-file association before permanent ID is known.
        /// </summary>
        public string? ClientMessageId { get; private set; } // Needs to be settable, at least initially

        /// <summary>
        /// EF Core 使用的私有构造函数。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private Message() : base() { } // 调用基类构造函数
#pragma warning restore CS8618

        /// <summary>
        /// 内部构造函数，用于创建消息实例。
        /// </summary>
        private Message(Guid senderId, Guid recipientId, MessageRecipientType recipientType, string content, MessageType type, Guid? replyToMessageId = null, string? clientMessageId = null)
        {
            // Id 和 CreatedAt (SentAt) 由基类构造函数设置
            if (senderId == Guid.Empty)
                throw new ArgumentException("Sender ID cannot be empty.", nameof(senderId));
            if (recipientId == Guid.Empty)
                throw new ArgumentException("Recipient ID cannot be empty.", nameof(recipientId));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty.", nameof(content));
            if (content.Length > ContentMaxLength)
                throw new ArgumentException($"Message content cannot exceed {ContentMaxLength} characters.", nameof(content));
            if (!Enum.IsDefined(typeof(MessageType), type))
                throw new ArgumentException("Invalid message type.", nameof(type));
            if (replyToMessageId.HasValue && replyToMessageId.Value == Guid.Empty)
                throw new ArgumentException("ReplyToMessageId cannot be an empty GUID if provided.", nameof(replyToMessageId));

            CreatedBy = senderId; // 将发送者ID设置为 CreatedBy
            RecipientId = recipientId;
            RecipientType = recipientType;
            Content = content;
            Type = type;
            ReplyToMessageId = replyToMessageId;
            ClientMessageId = clientMessageId; // Set ClientMessageId
            IsRecalled = false; // Default to not recalled
            RecalledAt = null;
            LastModifiedAt = CreatedAt; // 初始时 LastModifiedAt 等于 CreatedAt
            LastModifiedBy = senderId;  // 初始修改者即为发送者
        }

        /// <summary>
        /// 标记消息为已送达。
        /// </summary>
        /// <param name="modifierId">执行此操作的用户ID（通常为系统或接收者，可选）。</param>
        public void MarkAsDelivered(Guid? modifierId = null)
        {
            if (!DeliveredAt.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                DeliveredAt = now;
                LastModifiedAt = now;
                LastModifiedBy = modifierId;
            }
        }

        /// <summary>
        /// 创建一条发送给单个用户的消息。
        /// </summary>
        /// <param name="sender">发送者用户。</param>
        /// <param name="recipientUser">接收者用户。</param>
        /// <param name="content">消息内容。</param>
        /// <param name="type">消息类型。</param>
        /// <param name="replyToMessage">被回复的消息（可选）。</param>
        /// <returns>新的消息实例。</returns>
        public static Message CreateUserMessage(User sender, User recipientUser, string content, MessageType type, Message? replyToMessage = null)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender), "Sender cannot be null.");
            if (recipientUser == null) throw new ArgumentNullException(nameof(recipientUser), "Recipient user cannot be null.");
            // 其他验证（如 content, type）已移至私有构造函数

            return new Message(sender.Id, recipientUser.Id, MessageRecipientType.User, content, type, replyToMessage?.Id);
        }

        /// <summary>
        /// 创建一条发送给群组的消息。
        /// </summary>
        /// <param name="sender">发送者用户。</param>
        /// <param name="recipientGroup">接收者群组。</param>
        /// <param name="content">消息内容。</param>
        /// <param name="type">消息类型。</param>
        /// <param name="replyToMessage">被回复的消息（可选）。</param>
        /// <returns>新的消息实例。</returns>
        public static Message CreateGroupMessage(User sender, Group recipientGroup, string content, MessageType type, Message? replyToMessage = null)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender), "Sender cannot be null.");
            if (recipientGroup == null) throw new ArgumentNullException(nameof(recipientGroup), "Recipient group cannot be null.");

            return new Message(sender.Id, recipientGroup.Id, MessageRecipientType.Group, content, type, replyToMessage?.Id);
        }

        /// <summary>
        /// 创建系统消息并自动添加相应的 MessageSentEvent 事件。
        /// </summary>
        /// <param name="senderId">发送者ID（系统消息通常显示为来自另一方）</param>
        /// <param name="recipientId">接收者ID</param>
        /// <param name="content">消息内容</param>
        /// <param name="senderUsername">发送者用户名（用于事件）</param>
        /// <param name="senderAvatarUrl">发送者头像URL（用于事件，可选）</param>
        /// <returns>新创建的系统消息</returns>
        public static Message CreateSystemMessage(
            Guid senderId, 
            Guid recipientId, 
            string content,
            string? senderUsername = null,
            string? senderAvatarUrl = null)
        {
            if (senderId == Guid.Empty) throw new ArgumentException("Sender ID cannot be empty.", nameof(senderId));
            if (recipientId == Guid.Empty) throw new ArgumentException("Recipient ID cannot be empty.", nameof(recipientId));
            
            var message = new Message(
                senderId, 
                recipientId, 
                MessageRecipientType.User, 
                content, 
                MessageType.System
            );
            
            // 自动添加 MessageSentEvent 领域事件
            message.AddDomainEvent(new MessageSentEvent(
                messageId: message.Id,
                senderId: senderId,
                recipientId: recipientId,
                recipientType: MessageRecipientType.User,
                messageContentPreview: content,
                senderUsername: senderUsername,
                senderAvatarUrl: senderAvatarUrl,
                groupName: null // 不是群组消息
            ));
            
            return message;
        }

        /// <summary>
        /// Recalls the message.
        /// </summary>
        /// <param name="actorId">The ID of the user recalling the message (must be the sender).</param>
        /// <param name="recallTimeLimit">The time limit within which a message can be recalled (e.g., 2 minutes).</param>
        /// <returns>True if the message was successfully recalled, false otherwise (e.g., time limit exceeded, already recalled, or not sender).</returns>
        public bool Recall(Guid actorId, TimeSpan recallTimeLimit)
        {
            if (IsRecalled)
            {
                return false;
            }

            if (CreatedBy != actorId)
            {
                return false;
            }

            if (DateTimeOffset.UtcNow > CreatedAt.Add(recallTimeLimit))
            {
                return false;
            }

            IsRecalled = true;
            RecalledAt = DateTimeOffset.UtcNow;
            this.Content = "[消息已撤回]";
            this.Type = MessageType.System;

            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = actorId;
            
            return true;
        }

        /// <summary>
        /// Updates the content and type of the message, typically used after a file upload is confirmed.
        /// </summary>
        public void UpdateContentAndType(string newContent, MessageType newType, Guid modifierId)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("New message content cannot be empty.", nameof(newContent));
            if (newContent.Length > ContentMaxLength)
                throw new ArgumentException($"Message content cannot exceed {ContentMaxLength} characters.", nameof(newContent));
            if (!Enum.IsDefined(typeof(MessageType), newType))
                throw new ArgumentException("Invalid new message type.", nameof(newType));
            if (modifierId == Guid.Empty)
                throw new ArgumentException("Modifier ID cannot be empty.", nameof(modifierId));

            Content = newContent;
            Type = newType;
            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = modifierId;
        }
    }
}