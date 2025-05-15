using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 配置 <see cref="Friendship"/> 实体的数据库映射。
    /// </summary>
    public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
    {
        /// <summary>
        /// 配置实体类型 <see cref="Friendship"/> 的数据库映射。
        /// </summary>
        /// <param name="builder">用于配置实体类型的生成器。</param>
        public void Configure(EntityTypeBuilder<Friendship> builder)
        {
            builder.ToTable("Friendships");

            builder.HasKey(f => f.Id);

            // 配置与 Requester (User) 的关系
            builder.HasOne(f => f.Requester)
                   .WithMany() // User 实体没有直接导航回其发起的 Friendship 列表
                   .HasForeignKey(f => f.CreatedBy) // RequesterId is now CreatedBy
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict); // 防止因删除用户而意外删除好友关系记录，业务逻辑应先处理

            // 配置与 Addressee (User) 的关系
            builder.HasOne(f => f.Addressee)
                   .WithMany() // User 实体没有直接导航回其收到的 Friendship 列表
                   .HasForeignKey(f => f.AddresseeId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict); // 同上

            builder.Property(f => f.Status)
                .IsRequired()
                .HasConversion<string>() // 将枚举存储为字符串
                .HasMaxLength(20);

            builder.Property(f => f.RequesterRemark)
                .HasMaxLength(255); // 允许备注为空，设置一个合理的最大长度

            builder.Property(f => f.AddresseeRemark)
                .HasMaxLength(255); // 允许备注为空，设置一个合理的最大长度

            // CreatedAt 和 UpdatedAt 由 AuditableEntity 处理

            builder.Property(f => f.BlockedById)
                .HasColumnName("BlockedById");

            builder.Property(f => f.BlockedAt)
                .HasColumnName("BlockedAt");

            // 确保 (CreatedBy, AddresseeId) 组合是唯一的，防止重复的好友请求
            // 注意：如果允许双向关系（A->B 和 B->A 是不同的记录），则此唯一索引可能不适用或需要调整。
            // 通常，好友关系是唯一的，无论方向。
            // 如果 (A,B) 和 (B,A) 视为同一关系，则需要在应用层保证只创建一条记录，
            // 例如，总是将ID较小的用户作为创建者 (CreatedBy)。
            // 此处假设 (CreatedBy, AddresseeId) 必须唯一。
            builder.HasIndex(f => new { f.CreatedBy, f.AddresseeId }).IsUnique(); // RequesterId is now CreatedBy
        }
    }
}