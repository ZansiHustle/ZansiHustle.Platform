namespace ZansiHustle.Application.Communications.PhoneVerification;

/// <summary>
/// Converts user-entered phone numbers to E.164 (e.g. "+27791234567") with
/// South-African-specific shortcuts. Twilio Verify rejects anything that is
/// not E.164, so every send/verify call must go through this first.
///
/// Handled inputs:
///   "0791234567"     → "+27791234567"   (SA local with leading 0)
///   "27791234567"    → "+27791234567"   (SA international without "+")
///   "+27791234567"   → "+27791234567"   (already E.164, passthrough)
///   "+1 415 555 0100" → "+14155550100"  (any country, whitespace/punct stripped)
///
/// Rejects: empty input, non-digit garbage, anything below 8 digits.
/// </summary>
public static class PhoneNumberNormalizer
{
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // Strip whitespace and common formatting characters. Anything else
        // is treated as invalid below — we don't try to be clever.
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (c == '-' || c == '(' || c == ')' || c == '.') continue;
            sb.Append(c);
        }
        var stripped = sb.ToString();
        if (stripped.Length == 0) return false;

        // Already E.164 — validate and passthrough.
        if (stripped[0] == '+')
        {
            var rest = stripped[1..];
            if (rest.Length < 8 || !AllDigits(rest)) return false;
            normalized = stripped;
            return true;
        }

        if (!AllDigits(stripped)) return false;

        // SA local format: 10 digits starting with 0.
        if (stripped.Length == 10 && stripped[0] == '0')
        {
            normalized = "+27" + stripped[1..];
            return true;
        }

        // SA international without leading "+": 11 digits starting with 27.
        if (stripped.StartsWith("27") && stripped.Length >= 10)
        {
            normalized = "+" + stripped;
            return true;
        }

        // Any other country, digits only — assume the caller has included
        // the country code already and just forgot the "+".
        if (stripped.Length >= 8)
        {
            normalized = "+" + stripped;
            return true;
        }

        return false;
    }

    private static bool AllDigits(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (!char.IsDigit(s[i])) return false;
        }
        return true;
    }
}
