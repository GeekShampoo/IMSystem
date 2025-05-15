using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 配置 <see cref="Message"/> 实体的数据库映射。
    /// </summary>
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        /// <summary>
        /// 配置实体类型 <see cref="Message"/> 的数据库映射。
        /// </summary>
        /// <param name="builder">用于配置实体类型的生成器。</param>
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");

            builder.HasKey(m => m.Id);

            // 配置与 Sender (User) 的关系
            builder.HasOne(m => m.Sender)
                   .WithMany() // User 实体没有直接导航回其发送的 Message 列表
                   .HasForeignKey(m => m.CreatedBy) // SenderId is now CreatedBy
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict); // 发送者用户删除时，消息通常保留，或标记为匿名/已删除用户

            // RecipientId 可以是 UserId 或 GroupId，这需要更复杂的映射或由应用层保证其一致性。
            // EF Core 本身不直接支持基于鉴别器字段的多态外键。
            // 一种常见做法是：
            // 1. RecipientId 存储实际的 Guid。
            // 2. RecipientType (User/Group) 作为鉴别器。
            // 3. 导航属性 RecipientUser 和 RecipientGroup 分别配置，但只有一个会有关联。
            //    这可能需要在查询时进行判断。

            // 配置与 RecipientUser (User) 的可选关系
            builder.HasOne(m => m.RecipientUser)
                   .WithMany() // User 实体没有直接导航回其接收的私聊 Message 列表
                   .HasForeignKey(m => m.RecipientId) // 条件外键，仅当 RecipientType 为 User 时有效
                                                      // EF Core 可能需要更明确的配置来处理这种情况，
                                                      // 或者在应用层确保 RecipientId 与 RecipientType 匹配。
                                                      // 对于简单场景，EF Core 会尝试匹配，但复杂查询可能需要手动处理。
                   .IsRequired(false) // 允许为 null (当消息是群消息时)
                   .OnDelete(DeleteBehavior.Restrict); // 接收者用户删除时，消息通常保留

            // 配置与 RecipientGroup (Group) 的可选关系
            builder.HasOne(m => m.RecipientGroup)
                   .WithMany() // Group 实体没有直接导航回其接收的 Message 列表
                   .HasForeignKey(m => m.RecipientId) // 条件外键，仅当 RecipientType 为 Group 时有效
                   .IsRequired(false) // 允许为 null (当消息是用户消息时)
                   .OnDelete(DeleteBehavior.Cascade); // 群组删除时，其消息也应删除 (或标记为失效)

            builder.Property(m => m.RecipientType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(m => m.Content)
                .IsRequired(); // 内容长度限制可以在应用层或数据库层面设置

            builder.Property(m => m.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // SentAt 现为 CreatedAt, UpdatedAt 由 AuditableEntity 处理

            // 配置与 ReplyToMessage (Message) 的可选关系 (自引用)
            builder.HasOne(m => m.ReplyToMessage)
                   .WithMany() // Message 实体没有直接导航回回复此消息的列表
                   .HasForeignKey(m => m.ReplyToMessageId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.NoAction); // 避免循环或多重级联路径问题

            // 索引，用于优化查询
            builder.HasIndex(m => m.CreatedBy); // SenderId is now CreatedBy
            builder.HasIndex(m => new { m.RecipientId, m.RecipientType }); // 联合索引优化按接收者查询
            builder.HasIndex(m => m.CreatedAt); // SentAt is now CreatedAt
            builder.Property(m => m.SequenceNumber)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasIndex(m => m.SequenceNumber);

        }
    }
}