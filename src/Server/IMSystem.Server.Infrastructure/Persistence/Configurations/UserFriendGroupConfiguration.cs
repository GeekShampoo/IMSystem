using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 配置 <see cref="UserFriendGroup"/> (好友关系与好友分组的关联) 实体的数据库映射。
    /// </summary>
    public class UserFriendGroupConfiguration : IEntityTypeConfiguration<UserFriendGroup>
    {
        /// <summary>
        /// 配置实体类型 <see cref="UserFriendGroup"/> 的数据库映射。
        /// </summary>
        /// <param name="builder">用于配置实体类型的生成器。</param>
        public void Configure(EntityTypeBuilder<UserFriendGroup> builder)
        {
            builder.ToTable("UserFriendGroups");

            builder.HasKey(ufg => ufg.Id); // 使用独立主键

            // 配置与 User 的关系 (分组的拥有者)
            builder.HasOne(ufg => ufg.User) // User 导航属性仍然指向创建者
                   .WithMany() // User 实体没有直接导航回其 UserFriendGroup 列表
                   .HasForeignKey(ufg => ufg.CreatedBy) // UserId (grouping owner) is now CreatedBy
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict); // 用户删除时，不直接删除此关联，业务逻辑处理

            // 配置与 Friendship 的关系
            builder.HasOne(ufg => ufg.Friendship)
                   .WithMany() // Friendship 实体没有直接导航回其 UserFriendGroup 列表
                   .HasForeignKey(ufg => ufg.FriendshipId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade); // 如果好友关系被删除，则此分配也应删除

            // 配置与 FriendGroup 的关系
            builder.HasOne(ufg => ufg.FriendGroup)
                   .WithMany() // FriendGroup 实体没有直接导航回其 UserFriendGroup 列表
                   .HasForeignKey(ufg => ufg.FriendGroupId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade); // 如果好友分组被删除，则此分配也应删除

            // CreatedAt 和 UpdatedAt 由 AuditableEntity 处理

            // 确保一个好友在一个用户的分组中只出现一次
            // 即 (CreatedBy, FriendshipId, FriendGroupId) 组合应该是唯一的。
            // 由于我们有独立主键 Id，这个唯一性可以通过索引来保证。
            // 或者更严格地，一个 FriendshipId 在一个 CreatedBy (用户) 下只能属于一个 FriendGroupId。
            // 这取决于业务需求：一个好友是否可以同时属于一个用户的多个分组？
            // 假设一个好友在一个用户下只能属于一个分组。
            // 那么 (CreatedBy, FriendshipId) 应该是唯一的。
            // 此处假设 (CreatedBy, FriendshipId, FriendGroupId) 组合唯一，允许一个好友被分到多个组（尽管不常见）。
            // 如果一个好友只能在一个组，那么应该在 (CreatedBy, FriendshipId) 上加唯一索引。
            // 我们将假设一个好友在一个用户下只能被分配到一个分组。
            builder.HasIndex(ufg => new { ufg.CreatedBy, ufg.FriendshipId }).IsUnique() // UserId is now CreatedBy
                .HasAnnotation("SqlServer:Include", new[] { "FriendGroupId" }); // 包含 FriendGroupId 以便查询优化
        }
    }
}