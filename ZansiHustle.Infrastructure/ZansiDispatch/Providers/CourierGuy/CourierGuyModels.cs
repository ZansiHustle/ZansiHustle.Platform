using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZansiHustle.Infrastructure.ZansiDispatch.Providers.CourierGuy
{
    // ─────────────────────────────────────────────────────────────────────────
    // Courier Guy / Shiplogic REQUEST models. Serialised with snake_case
    // (see CourierGuyJson) to match the documented payloads. RESPONSE shapes
    // are NOT modelled — they're parsed defensively in CourierGuyProvider
    // because the sample responses aren't confirmed yet.
    // ─────────────────────────────────────────────────────────────────────────

    internal static class CourierGuyJson
    {
        /// <summary>snake_case, omit-null serializer for Courier Guy request bodies.</summary>
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    internal sealed class CgAddress
    {
        public string? Type { get; set; }
        public string? Company { get; set; }
        public string? StreetAddress { get; set; }
        public string? LocalArea { get; set; }
        public string? City { get; set; }
        public string? Zone { get; set; }
        public string? Country { get; set; }
        public string? Code { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
    }

    internal sealed class CgContact
    {
        public string? Name { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
    }

    internal sealed class CgParcel
    {
        public string? ParcelDescription { get; set; }
        public decimal SubmittedLengthCm { get; set; }
        public decimal SubmittedWidthCm { get; set; }
        public decimal SubmittedHeightCm { get; set; }
        public decimal SubmittedWeightKg { get; set; }
        public string? AlternativeTrackingReference { get; set; }
    }

    internal sealed class CgRatesRequest
    {
        public CgAddress CollectionAddress { get; set; } = new();
        public CgAddress DeliveryAddress { get; set; } = new();
        public List<CgParcel> Parcels { get; set; } = new();
        public decimal? DeclaredValue { get; set; }
        public string? CollectionMinDate { get; set; }
        public string? DeliveryMinDate { get; set; }
    }

    internal sealed class CgShipmentRequest
    {
        public CgAddress CollectionAddress { get; set; } = new();
        public CgContact CollectionContact { get; set; } = new();
        public CgAddress DeliveryAddress { get; set; } = new();
        public CgContact DeliveryContact { get; set; } = new();
        public List<CgParcel> Parcels { get; set; } = new();
        public decimal? DeclaredValue { get; set; }
        public string? CustomerReference { get; set; }
        public string? CustomerReferenceName { get; set; }
        public string? SpecialInstructionsCollection { get; set; }
        public string? SpecialInstructionsDelivery { get; set; }
        public string? CollectionMinDate { get; set; }
        public string? DeliveryMinDate { get; set; }
        public string? ServiceLevelCode { get; set; }
        public int? ServiceLevelId { get; set; }
        public bool MuteNotifications { get; set; }
    }

    internal sealed class CgCancelRequest
    {
        public string TrackingReference { get; set; } = string.Empty;
    }
}
