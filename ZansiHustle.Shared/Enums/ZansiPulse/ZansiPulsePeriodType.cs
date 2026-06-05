namespace ZansiHustle.Shared.Enums.ZansiPulse;

/// <summary>
/// Aggregation window for ZansiPulse period-based metrics
/// (category / region / search-term metrics) and snapshots. Stored as
/// <c>int</c>. A metric row's (<c>PeriodType</c>, <c>PeriodStart</c>,
/// <c>PeriodEnd</c>) tuple identifies the bucket it accumulates into.
/// </summary>
public enum ZansiPulsePeriodType
{
    Hourly = 1,
    Daily = 2,
    Weekly = 3,
    Monthly = 4,
}
