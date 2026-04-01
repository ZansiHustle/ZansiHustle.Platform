using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.BudgetTransactions;

namespace ZansiHustle.Infrastructure.Data.Configurations.BudgetTransactions
{
    /// <summary>
    /// Configures the database mapping for the <see cref="BudgetTransaction"/> entity.
    /// </summary>
    public class BudgetTransactionConfiguration : IEntityTypeConfiguration<BudgetTransaction>
    {
        /// <summary>
        /// Configures the entity.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        public void Configure(EntityTypeBuilder<BudgetTransaction> builder)
        {
            builder.ToTable("BudgetTransactions");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.TransactionDateUtc);
            builder.HasIndex(x => x.TransactionType);
            builder.HasIndex(x => x.Category);
            builder.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.TransactionType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Category)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Amount)
                .HasPrecision(18, 2);

            builder.Property(x => x.Reference)
                .HasMaxLength(150);

            builder.Property(x => x.RelatedEntityType)
                .HasMaxLength(120);

            builder.Property(x => x.TransactionDateUtc)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();
        }
    }
}
