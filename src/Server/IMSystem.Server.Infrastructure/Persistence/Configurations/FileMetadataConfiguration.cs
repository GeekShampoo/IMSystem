using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 配置 <see cref="FileMetadata"/> 实体的数据库映射。
    /// </summary>
    public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
    {
        /// <summary>
        /// 配置实体类型 <see cref="FileMetadata"/> 的数据库映射。
        /// </summary>
        /// <param name="builder">用于配置实体类型的生成器。</param>
        public void Configure(EntityTypeBuilder<FileMetadata> builder)
        {
            builder.ToTable("FileMetadatas");

            builder.HasKey(fm => fm.Id);

            builder.Property(fm => fm.FileName)
                .IsRequired()
                .HasMaxLength(255); // 常见文件名长度限制

            builder.Property(fm => fm.StoredFileName)
                .IsRequired()
                .HasMaxLength(1024); // 存储系统中的路径或键可能较长

            builder.Property(fm => fm.ContentType)
                .IsRequired()
                .HasMaxLength(100); // MIME 类型长度

            builder.Property(fm => fm.FileSize)
                .IsRequired();

            // 配置与 Uploader (User) 的关系
            // FileMetadata.Uploader 导航属性仍然存在
            // FileMetadata.CreatedBy (来自 AuditableEntity) 将是外键
            builder.HasOne(fm => fm.Uploader)
                   .WithMany() // User 实体没有直接导航回其上传的 FileMetadata 列表
                   .HasForeignKey(fm => fm.CreatedBy) // UploaderId is now CreatedBy from AuditableEntity
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict); // 用户删除时，其上传的文件元数据通常保留，或标记为匿名/已删除用户

            // UploadedAt is now CreatedAt, managed by AuditableEntity, no need to configure here unless overriding
            // builder.Property(fm => fm.CreatedAt) // Example if specific configuration needed
            //    .IsRequired();

            builder.Property(fm => fm.StorageProvider)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(fm => fm.AccessUrl)
                .HasMaxLength(2048); // URL 长度

            builder.Property(fm => fm.IsConfirmed)
                .IsRequired();

            // 索引
            builder.HasIndex(fm => fm.CreatedBy); // Index on CreatedBy (formerly UploaderId)
            builder.HasIndex(fm => fm.StoredFileName).IsUnique(); // 存储名应该是唯一的
        }
    }
}