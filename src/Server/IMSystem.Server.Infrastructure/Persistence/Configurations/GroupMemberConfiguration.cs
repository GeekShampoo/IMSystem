using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 配置 <see cref="GroupMember"/> 实体的数据库映射。
    /// </summary>
    public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
    {
        /// <summary>
        /// 配置实体类型 <see cref="GroupMember"/> 的数据库映射。
        /// </summary>
        /// <param name="builder">用于配置实体类型的生成器。</param>
        public void Configure(EntityTypeBuilder<GroupMember> builder)
        {
            builder.ToTable("GroupMembers");

            builder.HasKey(gm => gm.Id);

            // 配置与 Group 的关系 (已在 GroupConfiguration 中配置了另一端)
            builder.HasOne(gm => gm.Group)
                   .WithMany(g => g.Members)
                   .HasForeignKey(gm => gm.GroupId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade); // 如果 Group 被删除，成员关系也删除

            // 配置与 User 的关系
            builder.HasOne(gm => gm.User)
                   .WithMany() // User 实体没有直接导航回其参与的 GroupMember 列表
                   .HasForeignKey(gm => gm.UserId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict); // 如果 User 被删除，应由业务逻辑处理其群组成员关系（例如，将其从群组中移除），而不是级联删除

            builder.Property(gm => gm.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(gm => gm.NicknameInGroup)
                .HasMaxLength(50);

            // JoinedAt 现为 CreatedAt，UpdatedAt 由 AuditableEntity 处理

            // 确保一个用户在一个群组中只出现一次
            builder.HasIndex(gm => new { gm.GroupId, gm.UserId }).IsUnique();
        }
    }
}