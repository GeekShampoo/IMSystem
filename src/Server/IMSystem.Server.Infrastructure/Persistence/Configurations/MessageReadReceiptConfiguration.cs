using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations;

/// <summary>
/// MessageReadReceipt 实体的数据库映射配置。
/// </summary>
public class MessageReadReceiptConfiguration : IEntityTypeConfiguration<MessageReadReceipt>
{
    public void Configure(EntityTypeBuilder<MessageReadReceipt> builder)
    {
        builder.ToTable("MessageReadReceipts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.MessageId)
            .IsRequired();

        builder.Property(r => r.ReaderUserId)
            .IsRequired();

        builder.Property(r => r.ReadAt)
            .IsRequired();

        // 索引优化查询
        builder.HasIndex(r => new { r.MessageId, r.ReaderUserId }).IsUnique(); // 确保一个用户对一条消息只有一个已读回执
        builder.HasIndex(r => r.MessageId);
        builder.HasIndex(r => r.ReaderUserId);

        // 如果需要与 Message 和 User 实体建立外键关系 (可选，但推荐)
        // 这取决于 MessageReadReceipt 实体中是否定义了导航属性，并且是否希望EF Core管理这些关系。
        // 当前 MessageReadReceipt 实体中未定义导航属性，所以此处不添加外键配置。
        // 如果添加了导航属性，可以这样配置：
        // builder.HasOne<Message>() // 或者 builder.HasOne(r => r.Message) 如果有导航属性
        //     .WithMany() // 如果 Message 实体没有对应的已读回执集合导航属性
        //     .HasForeignKey(r => r.MessageId)
        //     .OnDelete(DeleteBehavior.Cascade); // 当消息删除时，相关的已读回执也删除

        // builder.HasOne<User>() // 或者 builder.HasOne(r => r.ReaderUser)
        //     .WithMany() // 如果 User 实体没有对应的已读回执集合导航属性
        //     .HasForeignKey(r => r.ReaderUserId)
        //     .OnDelete(DeleteBehavior.Cascade); // 当用户删除时，相关的已读回执也删除 (或者 Restrict)

        // AuditableEntity 的属性 (CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy) 会被自动处理或通过基类配置。
    }
}