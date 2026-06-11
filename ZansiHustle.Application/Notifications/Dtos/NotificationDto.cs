using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Notifications.Dtos
{
    /// <summary>
    /// Client-facing notification. <see cref="Type"/> is the enum name (stable
    /// string the mobile switches on); <see cref="Data"/> is the decoded
    /// deep-link payload (targetType + ids) so a tap can route without parsing.
    /// </summary>
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>Decoded DataJson, e.g.
        /// { "targetType":"SellerBooking", "bookingId":"…" }. Empty when none.</summary>
        public Dictionary<string, string> Data { get; set; } = new();
    }

    /// <summary>Notifications list plus the unread total (one round-trip).</summary>
    public class NotificationListDto
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int UnreadCount { get; set; }
    }

    /// <summary>Just the unread badge count.</summary>
    public class UnreadCountDto
    {
        public int UnreadCount { get; set; }
    }
}
