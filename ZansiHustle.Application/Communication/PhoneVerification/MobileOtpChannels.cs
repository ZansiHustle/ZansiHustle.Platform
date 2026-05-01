using ZansiHustle.Shared.Enums.Communications;

namespace ZansiHustle.Application.Communications.PhoneVerification;

/// <summary>
/// Single source of truth for converting between client-facing channel
/// strings and the internal <see cref="MobileOtpChannel"/> enum, and for
/// rendering the enum into the wire string Twilio Verify expects on
/// <c>CreateVerificationOptions.Channel</c>.
///
/// Why centralise: the client must NOT be able to pass arbitrary strings
/// through to Twilio (e.g. "voice", "email", or future channels we have
/// not vetted). Anything not in the allowlist is rejected at the API
/// boundary, so the rest of the codebase only ever deals with the enum.
/// </summary>
public static class MobileOtpChannels
{
    /// <summary>
    /// The default channel used when a client omits <c>channel</c> on
    /// <c>POST /api/auth/send-otp</c>. Keeps backward-compatibility with
    /// older mobile builds that pre-date the channel parameter.
    /// </summary>
    public const MobileOtpChannel Default = MobileOtpChannel.Sms;

    /// <summary>
    /// Parses an incoming, client-supplied channel string into the
    /// internal enum. Empty / null / whitespace falls back to <see cref="Default"/>.
    /// Returns <c>false</c> for any unrecognised string so the caller can
    /// reject the request with a stable error code rather than guessing.
    /// </summary>
    public static bool TryParse(string? input, out MobileOtpChannel channel)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            channel = Default;
            return true;
        }

        switch (input.Trim().ToLowerInvariant())
        {
            case "sms":
                channel = MobileOtpChannel.Sms;
                return true;
            case "whatsapp":
            case "whats_app":
            case "whats-app":
                channel = MobileOtpChannel.WhatsApp;
                return true;
            default:
                channel = Default;
                return false;
        }
    }

    /// <summary>
    /// Renders the channel into the wire string expected by Twilio Verify.
    /// This is the ONLY place in the codebase that should produce these
    /// strings — keeping it here means a future channel addition is a
    /// single edit.
    /// </summary>
    public static string ToTwilioChannel(MobileOtpChannel channel) => channel switch
    {
        MobileOtpChannel.Sms => "sms",
        MobileOtpChannel.WhatsApp => "whatsapp",
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unsupported mobile OTP channel.")
    };

    /// <summary>
    /// Stable, lowercase string for cache keys and logs. Distinct from
    /// <see cref="ToTwilioChannel"/> conceptually even if the values
    /// currently match — keeps log/cache stability decoupled from any
    /// future Twilio renaming.
    /// </summary>
    public static string ToWireString(MobileOtpChannel channel) => channel switch
    {
        MobileOtpChannel.Sms => "sms",
        MobileOtpChannel.WhatsApp => "whatsapp",
        _ => "unknown"
    };
}
