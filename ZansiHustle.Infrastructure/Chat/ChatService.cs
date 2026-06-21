using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Chat;
using ZansiHustle.Application.Chat.Dtos;
using ZansiHustle.Application.Realtime;
using ZansiHustle.Domain.Chat;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Chat;
using ZansiHustle.Shared.Enums.ServiceBookings;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Chat
{
    /// <summary>
    /// Chat domain logic.
    ///
    /// Why this lives in Infrastructure (not Application):
    /// the entire service is "EF queries against AppDbContext +
    /// validation". No external dependencies, no transport-specific
    /// glue. Forcing a repository indirection for a single feature
    /// would add files without adding clarity — same call the recent
    /// AgentPayoutService made. The IChatService interface still
    /// lives in Application so the API layer never touches EF
    /// concrete types.
    /// </summary>
    public sealed class ChatService : IChatService
    {
        private const int PreviewMaxLength = 200;
        private const int BodyMaxLength = 4000;
        private const int InboxMaxTake = 100;
        private const int MessagesMaxTake = 100;
        private const int UnreadCap = 99;

        /// <summary>SAST is UTC+2, no DST — fixed offset for friendly local times.</summary>
        private static readonly TimeSpan SaUtcOffset = TimeSpan.FromHours(2);

        private readonly AppDbContext _db;
        private readonly IRealtimeNotifier _realtime;
        private readonly ILogger<ChatService> _logger;

        public ChatService(AppDbContext db, IRealtimeNotifier realtime, ILogger<ChatService> logger)
        {
            _db = db;
            _realtime = realtime;
            _logger = logger;
        }

        // ── Start: marketplace ─────────────────────────────────────

        public async Task<Result<ConversationDetailDto>> StartMarketplaceConversationAsync(
            Guid callerUserId, Guid listingId, CancellationToken ct = default)
        {
            try
            {
                var listing = await _db.MarketplaceListings
                    .AsNoTracking()
                    .Where(l => l.Id == listingId)
                    .Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.Price,
                        l.OwnerUserId,
                        l.Status,
                        Image = l.Images
                            .OrderBy(i => i.SortOrder)
                            .Select(i => i.Url)
                            .FirstOrDefault(),
                    })
                    .FirstOrDefaultAsync(ct);

                if (listing is null)
                    return Result<ConversationDetailDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                if (listing.OwnerUserId == callerUserId)
                    return Result<ConversationDetailDto>.Failure(
                        ErrorCodes.Forbidden, "You can't message yourself about your own listing.");

                // Serializable so two simultaneous "Message seller" taps
                // from the same buyer cannot create two parallel
                // conversation rows. The (Type, ListingId, BuyerUserId)
                // unique-shape isn't enforced by an EF index (only a
                // non-unique IX) because filtered unique indexes have
                // edge cases across SQL flavours — we enforce the
                // invariant inside the transaction instead.
                await using var tx = await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);

                var existing = await _db.Conversations
                    .Where(c => c.Type == ConversationType.MarketplaceListing
                                && c.MarketplaceListingId == listingId
                                && c.BuyerUserId == callerUserId)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync(ct);

                Guid conversationId;
                if (existing != Guid.Empty)
                {
                    conversationId = existing;
                }
                else
                {
                    var now = DateTime.UtcNow;
                    var conversation = new Conversation
                    {
                        Id = Guid.NewGuid(),
                        Type = ConversationType.MarketplaceListing,
                        MarketplaceListingId = listingId,
                        BuyerUserId = callerUserId,
                        SellerUserId = listing.OwnerUserId,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                    };
                    _db.Conversations.Add(conversation);

                    _db.ConversationParticipants.Add(new ConversationParticipant
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        UserId = callerUserId,
                        CreatedAtUtc = now,
                    });
                    _db.ConversationParticipants.Add(new ConversationParticipant
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        UserId = listing.OwnerUserId,
                        CreatedAtUtc = now,
                    });

                    await _db.SaveChangesAsync(ct);
                    conversationId = conversation.Id;
                }

                await tx.CommitAsync(ct);

                var detail = await LoadDetailAsync(conversationId, ct);
                if (detail is null)
                    return Result<ConversationDetailDto>.Failure(
                        ErrorCodes.Exception, "Conversation could not be loaded after creation.");

                return Result<ConversationDetailDto>.Success(detail, "Conversation ready.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to start marketplace conversation. Listing {ListingId}, Caller {UserId}",
                    listingId, callerUserId);
                return Result<ConversationDetailDto>.Failure(
                    ErrorCodes.Exception, "An error occurred while starting the conversation.");
            }
        }

        // ── Start: order ───────────────────────────────────────────

        public async Task<Result<ConversationDetailDto>> StartOrderConversationAsync(
            Guid callerUserId, Guid orderId, CancellationToken ct = default)
        {
            try
            {
                var order = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == orderId)
                    .Select(o => new { o.Id, o.Code, o.BuyerUserId, o.MerchantId })
                    .FirstOrDefaultAsync(ct);

                if (order is null)
                    return Result<ConversationDetailDto>.Failure(ErrorCodes.NotFound, "Order not found.");

                // The "seller side" of an order is the merchant's
                // owner user. Resolve it server-side rather than
                // trusting the client to pass it.
                // `Merchant.OwnerUserId` is nullable on the entity
                // (some legacy seed rows have no owner). Treat null
                // as "no seller available" — same NotFound shape as
                // a missing merchant.
                var merchantOwnerIdNullable = await _db.Merchants
                    .AsNoTracking()
                    .Where(m => m.Id == order.MerchantId)
                    .Select(m => m.OwnerUserId)
                    .FirstOrDefaultAsync(ct);

                var merchantOwnerId = merchantOwnerIdNullable ?? Guid.Empty;
                if (merchantOwnerId == Guid.Empty)
                    return Result<ConversationDetailDto>.Failure(
                        ErrorCodes.NotFound, "Order seller not found.");

                // Authorisation: caller must be buyer or merchant owner.
                if (callerUserId != order.BuyerUserId && callerUserId != merchantOwnerId)
                    return Result<ConversationDetailDto>.Failure(
                        ErrorCodes.Forbidden, "You don't have access to this order's chat.");

                // Edge case: buyer == merchant owner (admin testing).
                // Block self-chat the same way marketplace does.
                if (order.BuyerUserId == merchantOwnerId)
                    return Result<ConversationDetailDto>.Failure(
                        ErrorCodes.Forbidden, "Buyer and seller are the same user.");

                await using var tx = await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);

                var existing = await _db.Conversations
                    .Where(c => c.Type == ConversationType.Order && c.OrderId == orderId)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync(ct);

                Guid conversationId;
                if (existing != Guid.Empty)
                {
                    conversationId = existing;
                }
                else
                {
                    var now = DateTime.UtcNow;
                    var conversation = new Conversation
                    {
                        Id = Guid.NewGuid(),
                        Type = ConversationType.Order,
                        OrderId = orderId,
                        BuyerUserId = order.BuyerUserId,
                        SellerUserId = merchantOwnerId,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                    };
                    _db.Conversations.Add(conversation);

                    _db.ConversationParticipants.Add(new ConversationParticipant
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        UserId = order.BuyerUserId,
                        CreatedAtUtc = now,
                    });
                    _db.ConversationParticipants.Add(new ConversationParticipant
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        UserId = merchantOwnerId,
                        CreatedAtUtc = now,
                    });

                    await _db.SaveChangesAsync(ct);
                    conversationId = conversation.Id;
                }

                await tx.CommitAsync(ct);

                var detail = await LoadDetailAsync(conversationId, ct);
                if (detail is null)
                    return Result<ConversationDetailDto>.Failure(
                        ErrorCodes.Exception, "Conversation could not be loaded after creation.");

                return Result<ConversationDetailDto>.Success(detail, "Conversation ready.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to start order conversation. Order {OrderId}, Caller {UserId}",
                    orderId, callerUserId);
                return Result<ConversationDetailDto>.Failure(
                    ErrorCodes.Exception, "An error occurred while starting the conversation.");
            }
        }

        // ── Interaction gate (marketplace reviews) ─────────────────

        /// <inheritdoc />
        public Task<bool> HasMarketplaceConversationAsync(
            Guid listingId, Guid userId, CancellationToken ct = default)
        {
            // The user "interacted" about the listing when they're either the
            // cached buyer on the conversation OR a participant row for it.
            // Either side qualifies (the seller can also review the buyer's
            // interaction surface, and the cached BuyerUserId is the fast path).
            return _db.Conversations
                .AsNoTracking()
                .Where(c => c.Type == ConversationType.MarketplaceListing
                            && c.MarketplaceListingId == listingId
                            && (c.BuyerUserId == userId
                                || c.Participants.Any(p => p.UserId == userId)))
                .AnyAsync(ct);
        }

        // ── Inbox ──────────────────────────────────────────────────

        public async Task<Result<List<ConversationListItemDto>>> GetInboxAsync(
            Guid callerUserId, int take = 50, CancellationToken ct = default)
        {
            try
            {
                if (take <= 0) take = 50;
                if (take > InboxMaxTake) take = InboxMaxTake;

                // Per-row shape:
                //   • the conversation itself
                //   • the OTHER participant's display name + avatar
                //   • the OTHER participant resolved from the
                //     ConversationParticipants table (so we don't
                //     depend on the cached BuyerUserId / SellerUserId
                //     columns having the right side for the caller)
                //   • unread count = messages newer than my
                //     LastReadAtUtc that weren't sent by me, capped
                //     at 99 so the inbox badge never has to display
                //     ridiculous numbers
                var myParticipations = await _db.ConversationParticipants
                    .AsNoTracking()
                    .Where(p => p.UserId == callerUserId && !p.IsArchived)
                    .Select(p => new
                    {
                        p.ConversationId,
                        p.LastReadAtUtc,
                    })
                    .ToListAsync(ct);

                if (myParticipations.Count == 0)
                    return Result<List<ConversationListItemDto>>.Success(
                        new List<ConversationListItemDto>(), "Inbox empty.");

                var conversationIds = myParticipations
                    .Select(p => p.ConversationId)
                    .ToList();

                var lastReadByConv = myParticipations
                    .ToDictionary(p => p.ConversationId, p => p.LastReadAtUtc);

                // Conversations + the OTHER participant id + the
                // anchor's display fields (marketplace listing or
                // order code), ordered by activity.
                var conversations = await _db.Conversations
                    .AsNoTracking()
                    .Where(c => conversationIds.Contains(c.Id))
                    .OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedAtUtc)
                    .Take(take)
                    .Select(c => new
                    {
                        c.Id,
                        c.Type,
                        c.MarketplaceListingId,
                        c.OrderId,
                        c.IsClosed,
                        c.CreatedAtUtc,
                        c.LastMessageAtUtc,
                        c.LastMessagePreview,
                        OtherUserId = c.Participants
                            .Where(p => p.UserId != callerUserId)
                            .Select(p => p.UserId)
                            .FirstOrDefault(),
                    })
                    .ToListAsync(ct);

                if (conversations.Count == 0)
                    return Result<List<ConversationListItemDto>>.Success(
                        new List<ConversationListItemDto>(), "Inbox empty.");

                // Resolve "other user" identity in one query.
                var otherUserIds = conversations
                    .Where(c => c.OtherUserId != Guid.Empty)
                    .Select(c => c.OtherUserId)
                    .Distinct()
                    .ToList();

                var users = await _db.Users
                    .AsNoTracking()
                    .Where(u => otherUserIds.Contains(u.Id))
                    .Select(u => new
                    {
                        u.Id,
                        Name = ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Trim(),
                    })
                    .ToListAsync(ct);
                var userNameById = users.ToDictionary(u => u.Id, u => u.Name);

                // Marketplace listing snapshot for the listing-typed
                // conversations.
                var listingIds = conversations
                    .Where(c => c.MarketplaceListingId.HasValue)
                    .Select(c => c.MarketplaceListingId!.Value)
                    .Distinct()
                    .ToList();

                var listings = await _db.MarketplaceListings
                    .AsNoTracking()
                    .Where(l => listingIds.Contains(l.Id))
                    .Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.Price,
                        Image = l.Images
                            .OrderBy(i => i.SortOrder)
                            .Select(i => i.Url)
                            .FirstOrDefault(),
                    })
                    .ToListAsync(ct);
                var listingById = listings.ToDictionary(x => x.Id, x => x);

                // Order code snapshot.
                var orderIds = conversations
                    .Where(c => c.OrderId.HasValue)
                    .Select(c => c.OrderId!.Value)
                    .Distinct()
                    .ToList();
                var orders = await _db.Orders
                    .AsNoTracking()
                    .Where(o => orderIds.Contains(o.Id))
                    .Select(o => new { o.Id, o.Code })
                    .ToListAsync(ct);
                var orderById = orders.ToDictionary(x => x.Id, x => x.Code);

                // Service-booking context for order-typed conversations (T5): so the
                // inbox can show "Haircut booking · 12 Jun, 10:00" instead of a raw
                // order code. One query; oldest-first → newest booking per order
                // wins the slot (orders are 1:1 with a booking in practice).
                var bookingByOrder = new Dictionary<Guid, ServiceBookingContext>();
                if (orderIds.Count > 0)
                {
                    var bookingRows = await _db.ServiceBookings
                        .AsNoTracking()
                        .Where(b => orderIds.Contains(b.OrderId))
                        .OrderBy(b => b.CreatedAtUtc)
                        .Select(b => new ServiceBookingContext
                        {
                            OrderId = b.OrderId,
                            BookingId = b.Id,
                            StartAtUtc = b.StartAtUtc,
                            Status = b.Status,
                            ServiceName = b.Listing != null ? b.Listing.Title : null,
                            ImageUrl = b.Listing != null ? b.Listing.Images.FirstOrDefault() : null,
                        })
                        .ToListAsync(ct);
                    foreach (var b in bookingRows) bookingByOrder[b.OrderId] = b;
                }

                // Unread counts — one query, grouped. Filter to messages
                // not sent by the caller and (a) the conversation has
                // no last-read yet OR (b) the message is newer than the
                // last-read.
                var unreadGroups = await _db.ChatMessages
                    .AsNoTracking()
                    .Where(m => conversationIds.Contains(m.ConversationId)
                                && m.SenderUserId != callerUserId)
                    .GroupBy(m => m.ConversationId)
                    .Select(g => new
                    {
                        ConversationId = g.Key,
                        Messages = g.Select(m => new { m.CreatedAtUtc }).ToList(),
                    })
                    .ToListAsync(ct);

                var unreadCountByConv = unreadGroups.ToDictionary(
                    g => g.ConversationId,
                    g =>
                    {
                        DateTime? lastRead = lastReadByConv.TryGetValue(g.ConversationId, out var lr) ? lr : null;
                        var count = lastRead.HasValue
                            ? g.Messages.Count(m => m.CreatedAtUtc > lastRead.Value)
                            : g.Messages.Count;
                        return count > UnreadCap ? UnreadCap : count;
                    });

                var result = conversations
                    .Select(c =>
                    {
                        userNameById.TryGetValue(c.OtherUserId, out var otherName);
                        unreadCountByConv.TryGetValue(c.Id, out var unread);

                        string? listingTitle = null;
                        string? listingImage = null;
                        decimal? listingPrice = null;
                        if (c.MarketplaceListingId.HasValue
                            && listingById.TryGetValue(c.MarketplaceListingId.Value, out var l))
                        {
                            listingTitle = l.Title;
                            listingImage = l.Image;
                            listingPrice = l.Price;
                        }

                        string? orderCode = null;
                        if (c.OrderId.HasValue && orderById.TryGetValue(c.OrderId.Value, out var oc))
                            orderCode = oc;

                        // Friendly display context (T5) — never lead with a raw code.
                        var booking = c.OrderId.HasValue && bookingByOrder.TryGetValue(c.OrderId.Value, out var bc)
                            ? bc : null;
                        var (contextType, contextTitle, contextSubtitle, contextImage,
                             serviceBookingId, bookingStartAtUtc) =
                            ResolveContext(c.Type, listingTitle, listingImage, booking, orderCode);

                        // Lifecycle (T4): service-booking status + closed flag.
                        string? bookingStatus = booking is not null ? NormaliseBookingStatus(booking.Status) : null;
                        bool isClosed = c.IsClosed
                            || (booking is not null && IsTerminalBooking(booking.Status));

                        return new ConversationListItemDto
                        {
                            Id = c.Id,
                            Type = c.Type,
                            OtherUserId = c.OtherUserId,
                            OtherUserName = string.IsNullOrWhiteSpace(otherName) ? "Unknown" : otherName,
                            MarketplaceListingId = c.MarketplaceListingId,
                            MarketplaceListingTitle = listingTitle,
                            MarketplaceListingImageUrl = listingImage,
                            MarketplaceListingPrice = listingPrice,
                            OrderId = c.OrderId,
                            OrderCode = orderCode,
                            ContextType = contextType,
                            ContextTitle = contextTitle,
                            ContextSubtitle = contextSubtitle,
                            ContextImageUrl = contextImage,
                            ServiceBookingId = serviceBookingId,
                            BookingStartAtUtc = bookingStartAtUtc,
                            ServiceBookingStatus = bookingStatus,
                            IsClosed = isClosed,
                            CreatedAtUtc = c.CreatedAtUtc,
                            LastMessageAtUtc = c.LastMessageAtUtc,
                            LastMessagePreview = c.LastMessagePreview,
                            UnreadCount = unread,
                        };
                    })
                    .ToList();

                return Result<List<ConversationListItemDto>>.Success(result, "Inbox retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load inbox for {UserId}", callerUserId);
                return Result<List<ConversationListItemDto>>.Failure(
                    ErrorCodes.Exception, "An error occurred while loading your inbox.");
            }
        }

        // ── Messages ───────────────────────────────────────────────

        public async Task<Result<MessagesPageDto>> GetMessagesAsync(
            Guid callerUserId, Guid conversationId, DateTime? before = null,
            int take = 50, CancellationToken ct = default)
        {
            try
            {
                if (!await IsParticipantAsync(callerUserId, conversationId, ct))
                    return Result<MessagesPageDto>.Failure(
                        ErrorCodes.NotFound, "Conversation not found.");

                if (take <= 0) take = 50;
                if (take > MessagesMaxTake) take = MessagesMaxTake;

                var query = _db.ChatMessages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == conversationId);

                if (before.HasValue)
                    query = query.Where(m => m.CreatedAtUtc < before.Value);

                // Fetch one extra to detect HasMore without a count
                // round-trip.
                var rows = await query
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Take(take + 1)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        ConversationId = m.ConversationId,
                        SenderUserId = m.SenderUserId,
                        SenderName = m.Sender != null
                            ? ((m.Sender.FirstName ?? string.Empty) + " " + (m.Sender.LastName ?? string.Empty)).Trim()
                            : null,
                        Body = m.Body,
                        Type = m.Type,
                        CreatedAtUtc = m.CreatedAtUtc,
                    })
                    .ToListAsync(ct);

                var hasMore = rows.Count > take;
                if (hasMore) rows.RemoveAt(rows.Count - 1);

                // Closed / read-only state (T5) so the thread can lock its composer.
                var conv = await _db.Conversations
                    .AsNoTracking()
                    .Where(c => c.Id == conversationId)
                    .Select(c => new { c.Type, c.OrderId, c.IsClosed })
                    .FirstOrDefaultAsync(ct);

                bool isClosed = conv?.IsClosed ?? false;
                string? closedReason = (conv?.IsClosed ?? false) ? "Closed" : null;
                if (conv is not null && conv.Type == ConversationType.Order && conv.OrderId.HasValue)
                {
                    var bookingStatus = await GetServiceBookingStatusForOrderAsync(conv.OrderId.Value, ct);
                    if (bookingStatus.HasValue && IsTerminalBooking(bookingStatus.Value))
                    {
                        isClosed = true;
                        closedReason = bookingStatus.Value.ToString(); // Completed / Cancelled / Rejected
                    }
                }

                return Result<MessagesPageDto>.Success(new MessagesPageDto
                {
                    Messages = rows,
                    HasMore = hasMore,
                    IsClosed = isClosed,
                    ClosedReason = closedReason,
                }, "Messages retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to load messages. Conversation {ConversationId}, Caller {UserId}",
                    conversationId, callerUserId);
                return Result<MessagesPageDto>.Failure(
                    ErrorCodes.Exception, "An error occurred while loading messages.");
            }
        }

        // ── Send ───────────────────────────────────────────────────

        public async Task<Result<MessageDto>> SendMessageAsync(
            Guid callerUserId, Guid conversationId, string body, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(body))
                    return Result<MessageDto>.Failure(ErrorCodes.BadRequest, "Message cannot be empty.");

                var trimmed = body.Trim();
                if (trimmed.Length > BodyMaxLength)
                    return Result<MessageDto>.Failure(
                        ErrorCodes.BadRequest, $"Message too long. Max {BodyMaxLength} characters.");

                if (!await IsParticipantAsync(callerUserId, conversationId, ct))
                    return Result<MessageDto>.Failure(
                        ErrorCodes.NotFound, "Conversation not found.");

                var conversation = await _db.Conversations
                    .Where(c => c.Id == conversationId)
                    .FirstOrDefaultAsync(ct);
                if (conversation is null)
                    return Result<MessageDto>.Failure(ErrorCodes.NotFound, "Conversation not found.");

                if (conversation.IsClosed)
                    return Result<MessageDto>.Failure(ErrorCodes.Forbidden, "This conversation is closed.");

                // T5/T6 — block sends on a service-booking chat once the booking is
                // terminal (Completed/Cancelled/Rejected). Keeps the parties from
                // arranging cash-in-person after the platform booking has closed.
                // Product orders (no booking) and marketplace chats are unaffected.
                if (conversation.Type == ConversationType.Order && conversation.OrderId.HasValue)
                {
                    var bookingStatus = await GetServiceBookingStatusForOrderAsync(conversation.OrderId.Value, ct);
                    if (bookingStatus.HasValue && IsTerminalBooking(bookingStatus.Value))
                        return Result<MessageDto>.Failure(
                            ErrorCodes.Conflict, "This chat is closed because the booking is no longer active.");
                }

                var now = DateTime.UtcNow;
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    SenderUserId = callerUserId,
                    Body = trimmed,
                    Type = MessageType.Text,
                    CreatedAtUtc = now,
                };
                _db.ChatMessages.Add(message);

                // Denormalised inbox cache. Preview is trimmed so the
                // inbox list never has to read the full message body.
                conversation.LastMessageAtUtc = now;
                conversation.UpdatedAtUtc = now;
                conversation.LastMessagePreview = trimmed.Length > PreviewMaxLength
                    ? trimmed[..PreviewMaxLength]
                    : trimmed;

                // The sender always "reads" their own message — bump
                // their LastReadAtUtc so unread count stays accurate.
                var myParticipant = await _db.ConversationParticipants
                    .Where(p => p.ConversationId == conversationId && p.UserId == callerUserId)
                    .FirstOrDefaultAsync(ct);
                if (myParticipant is not null)
                    myParticipant.LastReadAtUtc = now;

                await _db.SaveChangesAsync(ct);

                var senderName = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == callerUserId)
                    .Select(u => ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Trim())
                    .FirstOrDefaultAsync(ct);

                var dto = new MessageDto
                {
                    Id = message.Id,
                    ConversationId = message.ConversationId,
                    SenderUserId = message.SenderUserId,
                    SenderName = senderName,
                    Body = message.Body,
                    Type = message.Type,
                    CreatedAtUtc = message.CreatedAtUtc,
                };

                // Best-effort live delivery to the OTHER participant(s) — never the
                // sender. Drives the recipient's open thread (append) + chat-tab
                // badge without polling. A push failure never breaks the send.
                await PushMessageToRecipientsAsync(
                    conversationId, callerUserId, conversation, dto, ct);

                return Result<MessageDto>.Success(dto, "Message sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send message. Conversation {ConversationId}, Caller {UserId}",
                    conversationId, callerUserId);
                return Result<MessageDto>.Failure(
                    ErrorCodes.Exception, "An error occurred while sending the message.");
            }
        }

        // ── Mark read ──────────────────────────────────────────────

        public async Task<Result<MarkReadResultDto>> MarkReadAsync(
            Guid callerUserId, Guid conversationId, CancellationToken ct = default)
        {
            try
            {
                if (!await IsParticipantAsync(callerUserId, conversationId, ct))
                    return Result<MarkReadResultDto>.Failure(
                        ErrorCodes.NotFound, "Conversation not found.");

                var conversation = await _db.Conversations
                    .AsNoTracking()
                    .Where(c => c.Id == conversationId)
                    .Select(c => new { c.LastMessageAtUtc })
                    .FirstOrDefaultAsync(ct);

                var stamp = conversation?.LastMessageAtUtc ?? DateTime.UtcNow;

                var participant = await _db.ConversationParticipants
                    .Where(p => p.ConversationId == conversationId && p.UserId == callerUserId)
                    .FirstOrDefaultAsync(ct);
                if (participant is null)
                    return Result<MarkReadResultDto>.Failure(
                        ErrorCodes.NotFound, "Conversation not found.");

                if (!participant.LastReadAtUtc.HasValue || participant.LastReadAtUtc.Value < stamp)
                {
                    participant.LastReadAtUtc = stamp;
                    await _db.SaveChangesAsync(ct);
                }

                return Result<MarkReadResultDto>.Success(new MarkReadResultDto
                {
                    ConversationId = conversationId,
                    LastReadAtUtc = participant.LastReadAtUtc!.Value,
                }, "Marked read.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to mark read. Conversation {ConversationId}, Caller {UserId}",
                    conversationId, callerUserId);
                return Result<MarkReadResultDto>.Failure(
                    ErrorCodes.Exception, "An error occurred while marking the conversation read.");
            }
        }

        // ── Unread summary (chat-tab badge) ────────────────────────

        public async Task<Result<ChatUnreadSummaryDto>> GetUnreadSummaryAsync(
            Guid callerUserId, CancellationToken ct = default)
        {
            try
            {
                var (conversations, messages) = await ComputeUnreadSummaryAsync(callerUserId, ct);
                return Result<ChatUnreadSummaryDto>.Success(new ChatUnreadSummaryDto
                {
                    UnreadConversations = conversations,
                    UnreadMessages = messages,
                }, "Unread summary.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load chat unread summary for {UserId}", callerUserId);
                return Result<ChatUnreadSummaryDto>.Failure(
                    ErrorCodes.Exception, "An error occurred while loading your unread count.");
            }
        }

        /// <summary>
        /// (conversationsWithUnread, totalUnreadMessages) for a user, both capped.
        /// Same definition as the inbox per-row unread: messages newer than the
        /// caller's LastReadAtUtc that the caller did not send.
        /// </summary>
        private async Task<(int conversations, int messages)> ComputeUnreadSummaryAsync(
            Guid userId, CancellationToken ct)
        {
            var myParts = await _db.ConversationParticipants
                .AsNoTracking()
                .Where(p => p.UserId == userId && !p.IsArchived)
                .Select(p => new { p.ConversationId, p.LastReadAtUtc })
                .ToListAsync(ct);
            if (myParts.Count == 0) return (0, 0);

            var convIds = myParts.Select(p => p.ConversationId).ToList();
            var lastReadByConv = myParts.ToDictionary(p => p.ConversationId, p => p.LastReadAtUtc);

            var groups = await _db.ChatMessages
                .AsNoTracking()
                .Where(m => convIds.Contains(m.ConversationId) && m.SenderUserId != userId)
                .GroupBy(m => m.ConversationId)
                .Select(g => new { ConversationId = g.Key, Items = g.Select(m => m.CreatedAtUtc).ToList() })
                .ToListAsync(ct);

            int unreadConvs = 0, unreadMsgs = 0;
            foreach (var g in groups)
            {
                DateTime? lr = lastReadByConv.TryGetValue(g.ConversationId, out var v) ? v : null;
                var cnt = lr.HasValue ? g.Items.Count(t => t > lr.Value) : g.Items.Count;
                if (cnt > 0)
                {
                    unreadConvs++;
                    unreadMsgs += cnt;
                }
            }
            if (unreadConvs > UnreadCap) unreadConvs = UnreadCap;
            if (unreadMsgs > UnreadCap) unreadMsgs = UnreadCap;
            return (unreadConvs, unreadMsgs);
        }

        /// <summary>
        /// Best-effort live message delivery to every participant except the
        /// sender. Pushes the message (for an open thread to append) + the
        /// recipient's fresh unread-conversation count (for the chat-tab badge).
        /// Never throws — REST is the source of truth.
        /// </summary>
        private async Task PushMessageToRecipientsAsync(
            Guid conversationId, Guid senderUserId, Conversation conversation,
            MessageDto dto, CancellationToken ct)
        {
            try
            {
                var recipients = await _db.ConversationParticipants
                    .AsNoTracking()
                    .Where(p => p.ConversationId == conversationId && p.UserId != senderUserId)
                    .Select(p => p.UserId)
                    .ToListAsync(ct);
                if (recipients.Count == 0) return;

                var payload = new
                {
                    conversationId = conversationId.ToString(),
                    message = new
                    {
                        id = dto.Id.ToString(),
                        conversationId = dto.ConversationId.ToString(),
                        senderUserId = dto.SenderUserId?.ToString(),
                        senderName = dto.SenderName,
                        body = dto.Body,
                        type = (int)dto.Type,
                        createdAtUtc = dto.CreatedAtUtc,
                    },
                    preview = conversation.LastMessagePreview,
                    lastMessageAtUtc = conversation.LastMessageAtUtc,
                };

                foreach (var recipient in recipients)
                {
                    await _realtime.ConversationMessageReceivedAsync(recipient, payload);
                    var (convs, _) = await ComputeUnreadSummaryAsync(recipient, ct);
                    await _realtime.ConversationUnreadCountChangedAsync(recipient, convs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Chat] realtime message push failed for conversation {ConversationId}.", conversationId);
            }
        }

        /// <summary>Friendly inbox context per conversation — never leads with a
        /// raw order code. Service bookings show the service name + date/time.</summary>
        private static (string contextType, string? title, string? subtitle, string? image,
            Guid? bookingId, DateTime? bookingStart) ResolveContext(
            ConversationType type, string? listingTitle, string? listingImage,
            ServiceBookingContext? booking, string? orderCode)
        {
            if (booking is not null)
            {
                var name = string.IsNullOrWhiteSpace(booking.ServiceName)
                    ? "Service booking" : booking.ServiceName!;
                var local = booking.StartAtUtc + SaUtcOffset;
                var when = local.ToString("d MMM, HH:mm", CultureInfo.InvariantCulture);
                return ("ServiceBooking", name, $"Service booking · {when}",
                    booking.ImageUrl, booking.BookingId, booking.StartAtUtc);
            }
            if (type == ConversationType.MarketplaceListing)
                return ("MarketplaceListing",
                    string.IsNullOrWhiteSpace(listingTitle) ? "Marketplace listing" : listingTitle,
                    "Marketplace listing", listingImage, null, null);
            if (type == ConversationType.Order)
                // Product order — friendly lead; order code only as secondary metadata.
                return ("Order", "Order",
                    string.IsNullOrWhiteSpace(orderCode) ? null : $"Order {orderCode}", null, null, null);
            return ("Direct", null, null, null, null, null);
        }

        /// <summary>Lightweight projection of a service booking for inbox context.</summary>
        private sealed class ServiceBookingContext
        {
            public Guid OrderId { get; set; }
            public Guid BookingId { get; set; }
            public DateTime StartAtUtc { get; set; }
            public ServiceBookingStatus Status { get; set; }
            public string? ServiceName { get; set; }
            public string? ImageUrl { get; set; }
        }

        /// <summary>Terminal booking states close the linked conversation (T5).</summary>
        private static bool IsTerminalBooking(ServiceBookingStatus s) =>
            s == ServiceBookingStatus.Completed
            || s == ServiceBookingStatus.Cancelled
            || s == ServiceBookingStatus.Rejected;

        /// <summary>Legacy Confirmed → "Requested" for display; otherwise the enum name.</summary>
        private static string NormaliseBookingStatus(ServiceBookingStatus s) =>
            s == ServiceBookingStatus.Confirmed
                ? ServiceBookingStatus.Requested.ToString()
                : s.ToString();

        /// <summary>Latest service-booking status for an order (null if the order
        /// has no booking — i.e. a product order). Used by the send guard + the
        /// messages closed-state.</summary>
        private async Task<ServiceBookingStatus?> GetServiceBookingStatusForOrderAsync(
            Guid orderId, CancellationToken ct)
        {
            var rows = await _db.ServiceBookings
                .AsNoTracking()
                .Where(b => b.OrderId == orderId)
                .OrderByDescending(b => b.CreatedAtUtc)
                .Select(b => b.Status)
                .Take(1)
                .ToListAsync(ct);
            return rows.Count > 0 ? rows[0] : (ServiceBookingStatus?)null;
        }

        // ── Helpers ────────────────────────────────────────────────

        private Task<bool> IsParticipantAsync(Guid userId, Guid conversationId, CancellationToken ct) =>
            _db.ConversationParticipants
                .AsNoTracking()
                .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId, ct);

        private async Task<ConversationDetailDto?> LoadDetailAsync(Guid conversationId, CancellationToken ct)
        {
            var c = await _db.Conversations
                .AsNoTracking()
                .Where(x => x.Id == conversationId)
                .FirstOrDefaultAsync(ct);
            if (c is null) return null;

            var buyerName = c.BuyerUserId.HasValue
                ? await NameAsync(c.BuyerUserId.Value, ct)
                : null;
            var sellerName = c.SellerUserId.HasValue
                ? await NameAsync(c.SellerUserId.Value, ct)
                : null;

            string? listingTitle = null;
            string? listingImage = null;
            decimal? listingPrice = null;
            if (c.MarketplaceListingId.HasValue)
            {
                var listing = await _db.MarketplaceListings
                    .AsNoTracking()
                    .Where(l => l.Id == c.MarketplaceListingId.Value)
                    .Select(l => new
                    {
                        l.Title,
                        l.Price,
                        Image = l.Images
                            .OrderBy(i => i.SortOrder)
                            .Select(i => i.Url)
                            .FirstOrDefault(),
                    })
                    .FirstOrDefaultAsync(ct);
                if (listing is not null)
                {
                    listingTitle = listing.Title;
                    listingImage = listing.Image;
                    listingPrice = listing.Price;
                }
            }

            string? orderCode = null;
            if (c.OrderId.HasValue)
            {
                orderCode = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == c.OrderId.Value)
                    .Select(o => o.Code)
                    .FirstOrDefaultAsync(ct);
            }

            return new ConversationDetailDto
            {
                Id = c.Id,
                Type = c.Type,
                BuyerUserId = c.BuyerUserId,
                BuyerUserName = buyerName,
                SellerUserId = c.SellerUserId,
                SellerUserName = sellerName,
                MarketplaceListingId = c.MarketplaceListingId,
                MarketplaceListingTitle = listingTitle,
                MarketplaceListingImageUrl = listingImage,
                MarketplaceListingPrice = listingPrice,
                OrderId = c.OrderId,
                OrderCode = orderCode,
                CreatedAtUtc = c.CreatedAtUtc,
                LastMessageAtUtc = c.LastMessageAtUtc,
                LastMessagePreview = c.LastMessagePreview,
                IsClosed = c.IsClosed,
            };
        }

        private async Task<string?> NameAsync(Guid userId, CancellationToken ct)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Trim())
                .FirstOrDefaultAsync(ct);
        }
    }
}
