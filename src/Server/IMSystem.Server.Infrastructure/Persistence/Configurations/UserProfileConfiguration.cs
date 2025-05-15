using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // Set UserProfile.UserId as the primary key.
        // Since UserProfile.Id (from AuditableEntity) is set to UserId in the constructor,
        // we are effectively using UserId as the PK.
        // If UserProfile.Id was a separate auto-generated Guid, then UserId would be just an FK.
        // Given UserProfile.Id = UserId, this configuration ensures UserId is the PK.
        builder.HasKey(up => up.UserId);

        // Configure the one-to-one relationship between User and UserProfile.
        // User has one Profile, and Profile belongs to one User.
        // The foreign key is UserProfile.UserId, which references User.Id.
        builder.HasOne(up => up.User)
               .WithOne(u => u.Profile) // Navigation property in User entity
               .HasForeignKey<UserProfile>(up => up.UserId) // Foreign key in UserProfile
               .IsRequired() // A UserProfile must belong to a User
               .OnDelete(DeleteBehavior.Cascade); // When a User is deleted, their UserProfile is also deleted.

        // Configure properties for UserProfile
        builder.Property(up => up.Nickname)
            .HasMaxLength(100); // Example max length, adjust as needed

        builder.Property(up => up.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(up => up.Gender)
            .HasMaxLength(50) // e.g., "Male", "Female", "Other", "PreferNotToSay"
            .HasConversion(
                v => v.HasValue ? v.Value.ToString() : null,
                v => !string.IsNullOrEmpty(v) ? (GenderType)Enum.Parse(typeof(GenderType), v) : (GenderType?)null);

        // builder.Property(up => up.Region) // Removed as Region is now part of the Address value object
        //     .HasMaxLength(100);

        builder.Property(up => up.Bio)
            .HasMaxLength(500); // Example max length for bio

        // Configure the owned entity Address
        builder.OwnsOne(up => up.Address, addressBuilder =>
        {
            // By default, EF Core will map properties of Address to columns named Address_Street, Address_City, etc.
            // We can customize column names and other aspects if needed.
            // For example, to make them nullable if Address itself is nullable:
            addressBuilder.Property(a => a.Street).HasMaxLength(200).IsRequired(false);
            addressBuilder.Property(a => a.City).HasMaxLength(100).IsRequired(false);
            addressBuilder.Property(a => a.StateOrProvince).HasMaxLength(100).IsRequired(false); // Changed from State to StateOrProvince
            addressBuilder.Property(a => a.Country).HasMaxLength(100).IsRequired(false);
            addressBuilder.Property(a => a.ZipCode).HasMaxLength(20).IsRequired(false);
            
            // If Address properties should have specific column names different from Address_PropertyName:
            // addressBuilder.Property(a => a.Street).HasColumnName("ProfileStreet").HasMaxLength(200);
            // addressBuilder.Property(a => a.City).HasColumnName("ProfileCity").HasMaxLength(100);
            // ... and so on for other properties.
        });
    }
}