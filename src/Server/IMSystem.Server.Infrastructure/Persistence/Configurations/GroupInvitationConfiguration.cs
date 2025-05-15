using IMSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMSystem.Server.Infrastructure.Persistence.Configurations;

public class GroupInvitationConfiguration : IEntityTypeConfiguration<GroupInvitation>
{
    public void Configure(EntityTypeBuilder<GroupInvitation> builder)
    {
        builder.ToTable("GroupInvitations");

        builder.HasKey(gi => gi.Id);

        builder.Property(gi => gi.Message)
            .HasMaxLength(500); // Optional: Set a max length for the invitation message

        builder.Property(gi => gi.Status)
            .IsRequired()
            .HasConversion<string>() // Store enum as string for better readability in DB
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(gi => gi.Group)
            .WithMany() // Assuming Group does not have a direct navigation collection for Invitations
            .HasForeignKey(gi => gi.GroupId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // Or Restrict, depending on desired behavior when a group is deleted

        builder.HasOne(gi => gi.Inviter)
            .WithMany() // Assuming User does not have a direct navigation collection for SentInvitations
            .HasForeignKey(gi => gi.InviterId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict); // Prevent inviter deletion if they have pending invitations

        builder.HasOne(gi => gi.InvitedUser)
            .WithMany() // Assuming User does not have a direct navigation collection for ReceivedInvitations
            .HasForeignKey(gi => gi.InvitedUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict); // Prevent invited user deletion if they have pending invitations

        // Indexes
        builder.HasIndex(gi => new { gi.GroupId, gi.InvitedUserId, gi.Status })
            .IsUnique(false) // Depending on whether you allow multiple pending invites to the same user for the same group
            .HasDatabaseName("IX_GroupInvitations_Group_InvitedUser_Status");

        builder.HasIndex(gi => gi.InvitedUserId)
            .HasDatabaseName("IX_GroupInvitations_InvitedUserId");

        builder.HasIndex(gi => gi.InviterId)
            .HasDatabaseName("IX_GroupInvitations_InviterId");

        builder.HasIndex(gi => gi.Status)
            .HasDatabaseName("IX_GroupInvitations_Status");
    }
}