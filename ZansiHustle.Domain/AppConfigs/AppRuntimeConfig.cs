namespace ZansiHustle.Domain.AppConfigs;

/// <summary>
/// A single remotely-controlled application config / feature flag. One flexible
/// row type holds booleans now and can carry strings / numbers / JSON later
/// without a schema change, so new controls only need a seed row + a consumer.
///
/// Toggling a FEATURE on/off is <see cref="BooleanValue"/> (the admin switch).
/// <see cref="IsActive"/> marks the config ROW itself active (a soft-delete /
/// hide flag). The mobile app may only read rows where <see cref="IsPublic"/> is
/// true.
/// </summary>
public class AppRuntimeConfig
{
    public Guid Id { get; set; }

    /// <summary>Stable machine key the apps gate on, e.g. "productPurchasingEnabled".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Human label shown in the admin portal.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Grouping for the portal UI: Customer, Marketplace, Seller, Messaging, Payments, System.</summary>
    public string Category { get; set; } = "System";

    /// <summary>Boolean | String | Number | Json — drives which value column is authoritative.</summary>
    public string ValueType { get; set; } = "Boolean";

    /// <summary>Authoritative value for boolean flags (the on/off switch).</summary>
    public bool BooleanValue { get; set; } = true;

    public string? StringValue { get; set; }
    public double? NumberValue { get; set; }
    public string? JsonValue { get; set; }

    /// <summary>Customer-friendly title shown when the gated feature is disabled.</summary>
    public string? DisabledTitle { get; set; }

    /// <summary>Customer-friendly body shown when the gated feature is disabled.</summary>
    public string? DisabledMessage { get; set; }

    /// <summary>True if the mobile/public app is allowed to read this config.</summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>True if the config row is active (false = hidden / retired).</summary>
    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
