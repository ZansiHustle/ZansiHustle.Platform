using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.AppConfigs.Dtos
{
    /// <summary>
    /// Full admin view of a single config row (Portal Super Admin UI).
    /// </summary>
    public sealed class AppConfigAdminDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "System";
        public string ValueType { get; set; } = "Boolean";
        public bool BooleanValue { get; set; }
        public string? StringValue { get; set; }
        public double? NumberValue { get; set; }
        public string? JsonValue { get; set; }
        public string? DisabledTitle { get; set; }
        public string? DisabledMessage { get; set; }
        public bool IsPublic { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// Admin update payload. All fields optional — only supplied fields change.
    /// </summary>
    public sealed class UpdateAppConfigRequestDto
    {
        public bool? BooleanValue { get; set; }
        public string? StringValue { get; set; }
        public double? NumberValue { get; set; }
        public string? JsonValue { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? DisabledTitle { get; set; }
        public string? DisabledMessage { get; set; }
        public bool? IsPublic { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Public per-key entry the mobile app gates on.
    /// </summary>
    public sealed class PublicAppConfigEntryDto
    {
        public bool Enabled { get; set; } = true;
        public string? Title { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Public response: a key → entry map plus a coarse version/timestamp the
    /// mobile app can use to detect changes.
    /// </summary>
    public sealed class PublicAppConfigsResponseDto
    {
        public Dictionary<string, PublicAppConfigEntryDto> Configs { get; set; } = new();
        public long Version { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
