using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 配置 <see cref="OutboxMessage"/> 实体的数据库映射。
    /// </summary>
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        /// <summary>
        /// 配置实体类型 <see cref="OutboxMessage"/> 的数据库映射。
        /// </summary>
        /// <param name="builder">用于配置实体类型的生成器。</param>
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");

            builder.HasKey(om => om.Id);

            builder.Property(om => om.EventType)
                .IsRequired()
                .HasMaxLength(255); // 事件类型名称长度

            builder.Property(om => om.EventPayload)
                .IsRequired(); // 通常是 JSON 字符串，长度不限或根据数据库能力设置

            builder.Property(om => om.OccurredAt)
                .IsRequired();

            builder.Property(om => om.ProcessedAt)
                .IsRequired(false); // 允许为 NULL

            builder.Property(om => om.Error)
                .IsRequired(false); // 允许为 NULL

            builder.Property(om => om.RetryCount)
                .IsRequired();

            // 索引，用于 OutboxProcessorService 高效轮询未处理的事件
            builder.HasIndex(om => new { om.ProcessedAt, om.OccurredAt })
                   .HasFilter("[ProcessedAt] IS NULL"); // 仅索引未处理的事件，按发生时间排序
        }
    }
}