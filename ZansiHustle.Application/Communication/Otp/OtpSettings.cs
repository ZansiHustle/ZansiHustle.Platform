namespace ZansiHustle.Application.Communications.Otp;

/// <summary>
/// OTP policy options. Bound from the "Otp" configuration section.
/// </summary>
public sealed class OtpSettings
{
    public const string SectionName = "Otp";

    /// <summary>Number of digits in the generated code. Default 6.</summary>
    public int CodeLength { get; set; } = 6;

    /// <summary>Time-to-live in seconds before an issued code expires. Default 300 (5 minutes).</summary>
    public int TtlSeconds { get; set; } = 300;

    /// <summary>Maximum verification attempts per issued code. Default 5.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Minimum seconds between successive issue requests for the same destination/purpose. Default 30.</summary>
    public int ResendCooldownSeconds { get; set; } = 30;
}
