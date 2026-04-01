using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.SellerLeads;

namespace ZansiHustle.Infrastructure.Data.Configurations.SellerLeads
{
    /// <summary>
    /// Configures the database mapping for the <see cref="SellerLead"/> entity.
    /// </summary>
    public class SellerLeadConfiguration : IEntityTypeConfiguration<SellerLead>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<SellerLead> builder)
        {
            builder.ToTable("SellerLeads");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.BusinessName);
            builder.HasIndex(x => x.ContactName);
            builder.HasIndex(x => x.Email);
            builder.HasIndex(x => x.PhoneNumber);
            builder.HasIndex(x => x.AssignedUserId);
            builder.HasIndex(x => x.LeadType);
            builder.HasIndex(x => x.VerificationStatus);
            builder.HasIndex(x => x.ApprovalStatus);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.ContactName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.BusinessName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Category)
                .HasMaxLength(120);

            builder.Property(x => x.Subcategory)
                .HasMaxLength(120);

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(30);

            builder.Property(x => x.Email)
                .HasMaxLength(256);

            builder.Property(x => x.Province)
                .HasMaxLength(150);

            builder.Property(x => x.City)
                .HasMaxLength(150);

            builder.Property(x => x.SocialHandleOrLink)
                .HasMaxLength(500);

            builder.Property(x => x.SourceType)
                .HasMaxLength(100);

            builder.Property(x => x.Notes)
                .HasMaxLength(3000);

            builder.Property(x => x.LeadType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.VerificationStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.ApprovalStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.SubmittedAtUtc)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.AssignedUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

