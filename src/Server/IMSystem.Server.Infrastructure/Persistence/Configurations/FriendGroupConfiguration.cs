using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 配置 <see cref="FriendGroup"/> 实体的数据库映射。
    /// </summary>
    public class FriendGroupConfiguration : IEntityTypeConfiguration<FriendGroup>
    {
        /// <summary>
        /// 配置实体类型 <see cref="FriendGroup"/> 的数据库映射。
        /// </summary>
        /// <param name="builder">用于配置实体类型的生成器。</param>
        public void Configure(EntityTypeBuilder<FriendGroup> builder)
        {
            builder.ToTable("FriendGroups");

            builder.HasKey(fg => fg.Id);

            builder.Property(fg => fg.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(fg => fg.Order)
                .IsRequired();

            builder.Property(fg => fg.IsDefault)
                .IsRequired()
                .HasDefaultValue(false); // 明确指定数据库默认值

            // CreatedAt 和 UpdatedAt 由 AuditableEntity 处理

            // 配置与 User 的一对多关系
            // FriendGroup 属于一个 User (创建者)
            builder.HasOne(fg => fg.User) // User 导航属性仍然指向创建者
                   .WithMany() // 在 User 实体中，我们没有定义 FriendGroups 集合的导航属性，所以这里用无参数的 WithMany()
                               // 如果 User 实体有 public virtual ICollection<FriendGroup> FriendGroups { get; set; }
                               // 则可以用 .WithMany(u => u.FriendGroups)
                   .HasForeignKey(fg => fg.CreatedBy) // UserId 现为 CreatedBy
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict); // 当用户被删除时，应由业务逻辑处理其好友分组，而不是级联删除

            // 索引 CreatedBy 和 Name，确保同一用户下的分组名唯一 (如果需要)
            builder.HasIndex(fg => new { fg.CreatedBy, fg.Name }).IsUnique(); // UserId 现为 CreatedBy

            // 索引 CreatedBy 和 IsDefault，可以用于快速查找用户的默认分组，
            // 并可以考虑添加唯一约束确保一个用户只有一个 IsDefault=true 的分组。
            // 但唯一性约束 (一个用户只有一个默认分组) 通常在业务逻辑层面更好控制，
            // 因为数据库级别的唯一约束对 IsDefault=false 的情况不适用。
            // 如果要用数据库约束，可能需要过滤索引 (Filtered Index) Where IsDefault = true。
            // 例如: builder.HasIndex(fg => new { fg.CreatedBy, fg.IsDefault }).IsUnique().HasFilter("[IsDefault] = 1");
            // 暂时只添加普通索引，唯一性由业务逻辑保证。
            builder.HasIndex(fg => new { fg.CreatedBy, fg.IsDefault });
        }
    }
}