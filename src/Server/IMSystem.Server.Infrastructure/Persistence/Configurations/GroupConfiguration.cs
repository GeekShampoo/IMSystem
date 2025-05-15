using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 配置 <see cref="Group"/> 实体的数据库映射。
    /// </summary>
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        /// <summary>
        /// 配置实体类型 <see cref="Group"/> 的数据库映射。
        /// </summary>
        /// <param name="builder">用于配置实体类型的生成器。</param>
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable("Groups");

            builder.HasKey(g => g.Id);

            builder.Property(g => g.Name)
                .IsRequired()
                .HasMaxLength(100);
            // 考虑群名是否需要全局唯一或在某个范围内唯一 (例如，用户创建的群名不能重复)
            // 此处不加唯一约束，具体业务逻辑可在应用层处理

            builder.Property(g => g.Description)
                .HasMaxLength(500);

            builder.Property(g => g.AvatarUrl)
                .HasMaxLength(2048);

            // 配置与 Owner (User) 的关系
            builder.HasOne(g => g.Owner)
                   .WithMany() // User 实体没有直接导航回其拥有的 Group 列表
                   .HasForeignKey(g => g.OwnerId) // 确保 Group.Owner 指向当前群主
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict); // 群主被删除时，群组不应自动删除，应由业务逻辑处理（例如转让群主或解散群）

           // CreatedAt 和 UpdatedAt 由 AuditableEntity 处理

            // 配置与 GroupMember 的一对多关系
            // Group 包含多个 GroupMember
            builder.HasMany(g => g.Members)
                   .WithOne(gm => gm.Group)
                   .HasForeignKey(gm => gm.GroupId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade); // 群组删除时，其成员关系也应删除
        }
    }
}