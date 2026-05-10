using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Reviews;

namespace ZansiHustle.Infrastructure.Persistence.Reviews
{
    /// <summary>
    /// EF Core mapping for <see cref="Review"/>. Auto-discovered by
    /// <c>ApplyConfigurationsFromAssembly</c> in <c>AppDbContext</c>.
    /// </summary>
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> entity)
        {
            entity.ToTable("Reviews");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TargetType).IsRequired();
            entity.Property(x => x.TargetId).IsRequired();
            entity.Property(x => x.ReviewerUserId).IsRequired();
            entity.Property(x => x.Rating).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.Property(x => x.Status).IsRequired();

            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc);
            entity.Property(x => x.DeletedAtUtc);

            // 1..5 — guards against bad clients silently writing 0 or
            // 6+ even when the service-layer validation is bypassed.
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Reviews_Rating_1_5",
                "[Rating] BETWEEN 1 AND 5"));

            // Lookup index for the most-common read path: fetch all
            // Active reviews for a given (TargetType, TargetId).
            entity.HasIndex(x => new { x.TargetType, x.TargetId, x.Status })
                .HasDatabaseName("IX_Reviews_Target_Status");

            // Reverse index for "did this user already review this
            // target?" — used by the create-guard and the summary's
            // MyReview projection.
            entity.HasIndex(x => new { x.ReviewerUserId, x.TargetType, x.TargetId, x.Status })
                .HasDatabaseName("IX_Reviews_Reviewer_Target_Status");

            // FK to AspNetUsers. NoAction because deleting the user
            // shouldn't cascade-blow-away every review they wrote on
            // public surfaces; the audit trail matters more than the
            // row count. If we ever want hard-deletion-on-user-delete
            // we can add a background job that flips Status to Deleted.
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ReviewerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
