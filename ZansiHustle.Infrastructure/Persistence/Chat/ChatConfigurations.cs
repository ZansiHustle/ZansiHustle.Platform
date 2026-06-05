using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZansiHustle.Domain.Chat;

namespace ZansiHustle.Infrastructure.Persistence.Chat
{
    // ─────────────────────────────────────────────────────────────────
    // EF configurations for the chat tables. Three entities:
    //   Conversations            (one row per chat thread)
    //   ConversationParticipants (one row per user-in-thread; v1 = 2)
    //   Messages                 (one row per text message)
    //
    // Indexes are chosen for the two hot read paths:
    //   • "show me my inbox"
    //       SELECT … FROM Conversations c
    //       JOIN ConversationParticipants p ON p.ConversationId = c.Id
    //       WHERE p.UserId = @me
    //       ORDER BY c.LastMessageAtUtc DESC
    //     → covered by IX_ConversationParticipants_UserId + IX_Conversations_LastMessageAtUtc.
    //   • "show me the messages in this thread"
    //       SELECT … FROM Messages WHERE ConversationId = @id ORDER BY CreatedAtUtc
    //     → covered by IX_Messages_ConversationId_CreatedAtUtc.
    //
    // FK delete behaviour is NoAction everywhere. Identity users are
    // soft-deactivated rather than hard-deleted (the existing pattern
    // — see AgentPayout / Engagement configurations), so cascading
    // would be silently destructive. If GDPR hard-delete ever lands,
    // a deliberate cleanup migration handles chat history.
    // ─────────────────────────────────────────────────────────────────

    public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.ToTable("Conversations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).IsRequired();

            // Bounded preview — the inbox renders one line per row, so
            // longer text would be cropped client-side anyway. Trim
            // server-side so the column stays scan-friendly.
            builder.Property(x => x.LastMessagePreview).HasMaxLength(200);

            // Inbox sort key. Most active threads first.
            builder.HasIndex(x => x.LastMessageAtUtc)
                .HasDatabaseName("IX_Conversations_LastMessageAtUtc");

            // Get-or-create lookup for "buyer ↔ this listing" — service
            // queries by (Type, MarketplaceListingId, BuyerUserId) to
            // dedupe. Filtered to MarketplaceListing rows so the index
            // stays small (only listing-typed conversations populate
            // MarketplaceListingId).
            builder.HasIndex(x => new { x.Type, x.MarketplaceListingId, x.BuyerUserId })
                .HasDatabaseName("IX_Conversations_Type_Listing_Buyer")
                .HasFilter("[MarketplaceListingId] IS NOT NULL");

            // Same shape for order-anchored conversations.
            builder.HasIndex(x => new { x.Type, x.OrderId })
                .HasDatabaseName("IX_Conversations_Type_Order")
                .HasFilter("[OrderId] IS NOT NULL");
        }
    }

    public sealed class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
    {
        public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
        {
            builder.ToTable("ConversationParticipants");
            builder.HasKey(x => x.Id);

            // One row per (conversation, user). Hard guarantee against
            // a duplicate participant insert under concurrency.
            builder.HasIndex(x => new { x.ConversationId, x.UserId })
                .IsUnique()
                .HasDatabaseName("IX_ConversationParticipants_Conversation_User");

            // Hot path: "all conversations I'm in". Index by user
            // alone since we always join back to Conversations after.
            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_ConversationParticipants_UserId");

            builder.HasOne(x => x.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Body)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(x => x.Type).IsRequired();

            // Primary message read: paginated thread view.
            builder.HasIndex(x => new { x.ConversationId, x.CreatedAtUtc })
                .HasDatabaseName("IX_Messages_ConversationId_CreatedAtUtc");

            builder.HasOne(x => x.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
