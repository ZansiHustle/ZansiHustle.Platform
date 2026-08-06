using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Blocks;

namespace ZansiHustle.Infrastructure.Persistence.Blocks
{
    /// <summary>EF mapping for <see cref="UserBlock"/>. Auto-discovered.</summary>
    public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
    {
        public void Configure(EntityTypeBuilder<UserBlock> entity)
        {
            entity.ToTable("UserBlocks");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.BlockerUserId).IsRequired();
            entity.Property(x => x.BlockedUserId).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500);

            // A user can only block another once.
            entity.HasIndex(x => new { x.BlockerUserId, x.BlockedUserId })
                .IsUnique()
                .HasDatabaseName("UX_UserBlocks_Blocker_Blocked");
            // Reverse lookup (is B blocked by anyone / by A?) for chat gating.
            entity.HasIndex(x => x.BlockedUserId)
                .HasDatabaseName("IX_UserBlocks_Blocked");
        }
    }
}
