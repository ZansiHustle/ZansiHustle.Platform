using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Agents.AgentApplications;

namespace ZansiHustle.Infrastructure.Data.Configurations.AgentApplications
{
    /// <summary>
    /// Configures the database mapping for the <see cref="AgentApplication"/> entity.
    /// </summary>
    public class AgentApplicationConfiguration : IEntityTypeConfiguration<AgentApplication>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<AgentApplication> builder)
        {
            builder.ToTable("AgentApplications");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Email);
            builder.HasIndex(x => x.PhoneNumber);
            builder.HasIndex(x => x.Status);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(30);

            builder.Property(x => x.Email)
                .HasMaxLength(256);

            builder.Property(x => x.Province)
                .HasMaxLength(150);

            builder.Property(x => x.City)
                .HasMaxLength(150);

            builder.Property(x => x.SocialHandle)
                .HasMaxLength(250);

            builder.Property(x => x.Notes)
                .HasMaxLength(2000);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.SubmittedAtUtc)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();
        }
    }
}
