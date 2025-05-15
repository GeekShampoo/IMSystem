using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Events; // 添加对 DomainEvent 命名空间的引用
using IMSystem.Server.Domain.Common; // For BaseEntity and DomainEvent
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Added for logging
using System.Reflection; // For applying configurations from assembly
using System.Text.Json; // For serializing domain events
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic; // For List

namespace IMSystem.Server.Infrastructure.Persistence
{
    /// <summary>
    /// 应用程序的 EF Core 数据库上下文。
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        private readonly ILogger<ApplicationDbContext> _logger;

        /// <summary>
        /// 初始化 <see cref="ApplicationDbContext"/> 类的新实例。
        /// </summary>
        /// <param name="options">用于配置此上下文的选项。</param>
        /// <param name="logger">日志记录器。</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ILogger<ApplicationDbContext> logger) : base(options)
        {
            _logger = logger;
            _logger.LogInformation("ApplicationDbContext: Constructor called.");
        }

        // 领域实体
        public DbSet<User> Users { get; set; }
        public DbSet<FriendGroup> FriendGroups { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<UserFriendGroup> UserFriendGroups { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<FileMetadata> FileMetadatas { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<MessageReadReceipt> MessageReadReceipts { get; set; }
        public DbSet<GroupInvitation> GroupInvitations { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _logger.LogInformation("ApplicationDbContext: OnModelCreating started.");
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Configure RowVersion for all entities inheriting from BaseEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<byte[]>("RowVersion")
                        .IsRowVersion();
                }
            }

            _logger.LogInformation("ApplicationDbContext: OnModelCreating finished.");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ApplicationDbContext: SaveChangesAsync (Overridden) started.");
            UpdateAuditableEntityProperties();
            await DispatchDomainEventsAsync();
            var result = await base.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("ApplicationDbContext: SaveChangesAsync (Overridden) finished, {Result} entities written.", result);
            return result;
        }

        public override int SaveChanges()
        {
            _logger.LogInformation("ApplicationDbContext: SaveChanges (Overridden) started.");
            UpdateAuditableEntityProperties();
            DispatchDomainEventsAsync().GetAwaiter().GetResult(); // Synchronous call for non-async SaveChanges
            var result = base.SaveChanges();
            _logger.LogInformation("ApplicationDbContext: SaveChanges (Overridden) finished, {Result} entities written.", result);
            return result;
        }

        private void UpdateAuditableEntityProperties()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is AuditableEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var auditableEntity = (AuditableEntity)entityEntry.Entity;
                var now = DateTimeOffset.UtcNow;

                if (entityEntry.State == EntityState.Added)
                {
                    // 实体构造函数应已设置 CreatedAt。此处可作为校验或回退。
                    if (auditableEntity.CreatedAt == default) 
                    {
                        auditableEntity.CreatedAt = now;
                    }
                }
                // 统一更新 LastModifiedAt
                auditableEntity.LastModifiedAt = now;
                // CreatedBy 和 LastModifiedBy 的主要设置责任在实体/应用层
            }
        }

        private async Task DispatchDomainEventsAsync()
        {
            var domainEventEntities = ChangeTracker.Entries<BaseEntity>()
                .Select(entry => entry.Entity)
                .Where(entity => entity.DomainEvents.Any())
                .ToList();

            if (!domainEventEntities.Any())
            {
                _logger.LogInformation("ApplicationDbContext: No domain events to dispatch.");
                return;
            }

            _logger.LogInformation("ApplicationDbContext: Found {Count} entities with domain events.", domainEventEntities.Count);

            var domainEvents = domainEventEntities
                .SelectMany(entity => entity.DomainEvents)
                .ToList();

            domainEventEntities.ForEach(entity => entity.ClearDomainEvents());

            var outboxMessages = new List<OutboxMessage>();
            foreach (var domainEvent in domainEvents)
            {
                var eventType = domainEvent.GetType().AssemblyQualifiedName;
                if (string.IsNullOrEmpty(eventType)) // 回退策略
                {
                    _logger.LogWarning("Could not get AssemblyQualifiedName for event type {EventActualTypeFullName}. Falling back to FullName.", domainEvent.GetType().FullName);
                    eventType = domainEvent.GetType().FullName ?? domainEvent.GetType().Name; 
                }

                var eventPayload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), new JsonSerializerOptions
                {
                    // 确保派生类型的属性能被序列化
                    // JsonSerializer.Serialize(object, Type) 的默认行为应该能处理这个问题
                });

                // 从领域事件中提取元数据，创建增强的 OutboxMessage
                var outboxMessage = new OutboxMessage(
                    eventType!,
                    eventPayload,
                    domainEvent.DateOccurred,
                    domainEvent.EventId,
                    domainEvent.Version,
                    domainEvent.EntityId,
                    domainEvent.TriggeredBy
                );

                outboxMessages.Add(outboxMessage);
                _logger.LogDebug("ApplicationDbContext: Created OutboxMessage for event {EventType} with ID {EventId}", outboxMessage.EventType, outboxMessage.EventId);
            }

            if (outboxMessages.Any())
            {
                await OutboxMessages.AddRangeAsync(outboxMessages);
                _logger.LogInformation("ApplicationDbContext: Added {Count} OutboxMessages to be saved.", outboxMessages.Count);
            }
        }
    }
}