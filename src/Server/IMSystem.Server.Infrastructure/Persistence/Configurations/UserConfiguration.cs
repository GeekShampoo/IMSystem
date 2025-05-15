using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 配置 <see cref="User"/> 实体的数据库映射。
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        /// <summary>
        /// 配置实体类型 <see cref="User"/> 的数据库映射。
        /// </summary>
        /// <param name="builder">用于配置实体类型的生成器。</param>
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);
            builder.HasIndex(u => u.Username).IsUnique();

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.Email)
                .HasMaxLength(100);
            builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL"); // 唯一约束，忽略 NULL 值

            // Nickname 和 ProfilePictureUrl 已移至 UserProfile 实体，相关配置应在 UserProfileConfiguration 中。
            // CreatedAt 和 UpdatedAt 由 AuditableEntity 处理

            // 导航属性的配置通常在关联的实体配置中定义另一端，
            // 或者如果需要更复杂的配置（如显式定义外键名），可以在这里配置。
            // 例如，User 与 FriendGroup 是一对多关系，FriendGroup 中有 CreatedBy 外键 (原 UserId)。
            // builder.HasMany(u => u.FriendGroups) // 如果 User 实体有 FriendGroups 集合
            //        .WithOne(fg => fg.User) // FriendGroup.User 指向创建者
            //        .HasForeignKey(fg => fg.CreatedBy)
            //        .OnDelete(DeleteBehavior.Cascade); // 用户删除时，其好友分组也删除
        }
    }
}