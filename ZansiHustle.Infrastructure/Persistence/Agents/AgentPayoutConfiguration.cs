using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Agents.AgentPayouts;
using ZansiHustle.Domain.Identity;

namespace ZansiHustle.Infrastructure.Persistence.Agents
{
    /// <summary>
    /// EF configuration for the agent payout ledger.
    ///
    /// Shape notes:
    ///   • Amount is decimal(18,2) — matches the money columns used on
    ///     payments / orders elsewhere.
    ///   • String fields are bounded so we never store unbounded admin
    ///     input (Note 1k, Reference 100, Method 50).
    ///   • Indexes target the two hot read paths the admin drawer and
    ///     the agent self-view need: by-agent (history list) and
    ///     by-agent-by-date (chronological history). The (AgentUserId,
    ///     PaidAtUtc DESC) shape is recreated server-side by SQL Server
    ///     using a descending index; EF generates this from the
    ///     IsDescending() hint.
    ///   • FK behaviour is NoAction on both sides (Agent + RecordedBy)
    ///     — agents are soft-deactivated, never hard-deleted, and we
    ///     don't want a stray hard-delete of the admin who recorded a
    ///     payout to silently wipe historical rows. If GDPR hard-delete
    ///     ever lands, payout rows need an explicit migration path.
    /// </summary>
    public sealed class AgentPayoutConfiguration : IEntityTypeConfiguration<AgentPayout>
    {
        public void Configure(EntityTypeBuilder<AgentPayout> builder)
        {
            builder.ToTable("AgentPayouts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.PaidAtUtc).IsRequired();
            builder.Property(x => x.RecordedAtUtc).IsRequired();
            builder.Property(x => x.AgentUserId).IsRequired();
            builder.Property(x => x.RecordedByUserId).IsRequired();

            builder.Property(x => x.PaymentReference).HasMaxLength(100);
            builder.Property(x => x.PaymentMethod).HasMaxLength(50);
            builder.Property(x => x.Note).HasMaxLength(1000);

            // Hot read path 1: "list all payouts for this agent". The
            // admin drawer and the agent self-view both filter by
            // AgentUserId and order by PaidAtUtc DESC, so the index
            // pairs both keys.
            builder.HasIndex(x => new { x.AgentUserId, x.PaidAtUtc })
                .HasDatabaseName("IX_AgentPayouts_AgentUserId_PaidAtUtc");

            // Hot read path 2: SUM(Amount) WHERE AgentUserId = … — the
            // outstanding-balance calculation runs every drawer open
            // and every record-payment call. Covered by the composite
            // above's leading column.

            builder.HasOne(x => x.AgentUser)
                .WithMany()
                .HasForeignKey(x => x.AgentUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.RecordedByUser)
                .WithMany()
                .HasForeignKey(x => x.RecordedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
