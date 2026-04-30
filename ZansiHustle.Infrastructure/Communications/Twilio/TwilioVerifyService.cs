using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio.Exceptions;
using Twilio.Rest.Verify.V2.Service;
using ZansiHustle.Application.Communications.PhoneVerification;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Communications.Twilio;

/// <summary>
/// Twilio Verify (V2) implementation of <see cref="IPhoneVerificationService"/>.
///
/// Why Verify instead of Programmable SMS:
///   - Twilio handles code generation, hashing, expiry, and check semantics —
///     we never store the code on our side, so there is no plaintext OTP to
///     leak from logs or memory dumps.
///   - Verify works over Twilio's pooled global senders, so we can ship OTP
///     today while waiting for the South African regulatory bundle to issue
///     a local long code. When the bundle is approved we either swap the
///     Verify Service's sender pool in the Twilio Console (zero code change)
///     or switch this implementation behind <see cref="IPhoneVerificationService"/>
///     for a local-sender provider.
///
/// Error handling:
///   - All Twilio calls are wrapped in try/catch.
///   - Provider-specific error strings are logged server-side but NEVER
///     returned to the client; callers always see a friendly generic message
///     plus a stable error code.
///   - A "verification not found" 404 on check is treated as <c>OTP_INVALID</c>
///     — Twilio returns this when the user never started a verification or
///     the code already expired/was consumed.
///
/// Rate limiting:
///   - A per-destination cooldown is enforced via <see cref="IMemoryCache"/>
///     before we call Twilio. Twilio also enforces its own rate limits, but
///     a cheap local check stops obvious spam without burning Verify credits.
/// </summary>
public sealed class TwilioVerifyService : IPhoneVerificationService
{
    private const string CacheKeyPrefix = "twilio-verify:cooldown:";

    private readonly ITwilioClientProvider _clientProvider;
    private readonly TwilioSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TwilioVerifyService> _logger;

    public TwilioVerifyService(
        ITwilioClientProvider clientProvider,
        IOptions<TwilioSettings> options,
        IMemoryCache cache,
        ILogger<TwilioVerifyService> logger)
    {
        _clientProvider = clientProvider;
        _settings = options.Value ?? new TwilioSettings();
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<SendOtpResult>> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (!PhoneNumberNormalizer.TryNormalize(phoneNumber, out var normalized))
            return Result<SendOtpResult>.Failure(ErrorCodes.InvalidPhoneNumber, "Please enter a valid phone number.");

        if (!TryGetReadyClient(out var client, out var configError))
            return Result<SendOtpResult>.Failure(configError!.Code, configError!.Message);

        var cooldown = Math.Max(0, _settings.Verify.ResendCooldownSeconds);
        var cooldownKey = CacheKeyPrefix + normalized;

        if (cooldown > 0 && _cache.TryGetValue<DateTime>(cooldownKey, out var nextAllowed))
        {
            var remaining = (int)Math.Ceiling((nextAllowed - DateTime.UtcNow).TotalSeconds);
            if (remaining > 0)
            {
                return Result<SendOtpResult>.Failure(
                    ErrorCodes.OtpResendCooldown,
                    $"Please wait {remaining} second(s) before requesting another code.");
            }
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new CreateVerificationOptions(
                pathServiceSid: _settings.Verify.ServiceSid,
                to: normalized,
                channel: "sms");

            var verification = await VerificationResource.CreateAsync(options, client);

            if (cooldown > 0)
            {
                var until = DateTime.UtcNow.AddSeconds(cooldown);
                _cache.Set(cooldownKey, until, until);
            }

            // Status is one of: "pending", "approved", "canceled".
            // On a fresh send we expect "pending" — anything else is unusual
            // but not necessarily fatal, so log and let the verify step decide.
            _logger.LogInformation(
                "Twilio Verify code dispatched. Sid={Sid} Status={Status} To={Phone}.",
                verification.Sid, verification.Status, MaskPhone(normalized));

            return Result<SendOtpResult>.Success(new SendOtpResult
            {
                PhoneNumber = normalized,
                ResendCooldownSeconds = cooldown
            }, "Verification code sent.");
        }
        catch (ApiException ex)
        {
            // Twilio-side rate limit (60-200 series) — surface as cooldown so
            // the mobile UI uses its existing "wait" copy instead of a generic
            // 502. Everything else is a generic phone-verification failure.
            if (ex.Status == 429 || ex.Code == 60203 /* max send attempts reached */)
            {
                _logger.LogWarning(ex,
                    "Twilio Verify rate-limited send for {Phone}. Code={Code} Status={Status}.",
                    MaskPhone(normalized), ex.Code, ex.Status);
                return Result<SendOtpResult>.Failure(
                    ErrorCodes.OtpResendCooldown,
                    "Too many requests. Please wait a moment and try again.");
            }

            _logger.LogError(ex,
                "Twilio Verify send failed for {Phone}. Code={Code} Status={Status} Message={Message}.",
                MaskPhone(normalized), ex.Code, ex.Status, ex.Message);
            return Result<SendOtpResult>.Failure(
                ErrorCodes.PhoneVerificationFailed,
                "We could not send the verification code. Please try again.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected Twilio Verify send failure for {Phone}.", MaskPhone(normalized));
            return Result<SendOtpResult>.Failure(
                ErrorCodes.PhoneVerificationFailed,
                "We could not send the verification code. Please try again.");
        }
    }

    public async Task<Result<VerifyOtpResult>> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        if (!PhoneNumberNormalizer.TryNormalize(phoneNumber, out var normalized))
            return Result<VerifyOtpResult>.Failure(ErrorCodes.InvalidPhoneNumber, "Please enter a valid phone number.");

        if (string.IsNullOrWhiteSpace(code))
            return Result<VerifyOtpResult>.Failure(ErrorCodes.OtpInvalid, "Verification code is required.");

        if (!TryGetReadyClient(out var client, out var configError))
            return Result<VerifyOtpResult>.Failure(configError!.Code, configError!.Message);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new CreateVerificationCheckOptions(_settings.Verify.ServiceSid)
            {
                To = normalized,
                Code = code.Trim()
            };

            var check = await VerificationCheckResource.CreateAsync(options, client);

            if (string.Equals(check.Status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Twilio Verify approved for {Phone}. Sid={Sid}.",
                    MaskPhone(normalized), check.Sid);

                return Result<VerifyOtpResult>.Success(new VerifyOtpResult
                {
                    PhoneNumber = normalized,
                    Verified = true
                }, "Phone number verified.");
            }

            _logger.LogInformation(
                "Twilio Verify rejected code for {Phone}. Status={Status}.",
                MaskPhone(normalized), check.Status);

            return Result<VerifyOtpResult>.Failure(
                ErrorCodes.OtpInvalid,
                "Invalid or expired verification code.");
        }
        catch (ApiException ex)
        {
            // 404: no pending verification — either never sent, already used,
            // or expired. Treat as invalid code so the client UX is consistent
            // ("invalid or expired"). Don't surface Twilio's wording.
            if (ex.Status == 404)
            {
                _logger.LogInformation(
                    "Twilio Verify check returned 404 for {Phone} — no pending verification.",
                    MaskPhone(normalized));
                return Result<VerifyOtpResult>.Failure(
                    ErrorCodes.OtpInvalid,
                    "Invalid or expired verification code.");
            }

            // 60202: max check attempts reached.
            if (ex.Code == 60202)
            {
                _logger.LogWarning(
                    "Twilio Verify max check attempts reached for {Phone}.", MaskPhone(normalized));
                return Result<VerifyOtpResult>.Failure(
                    ErrorCodes.OtpExhausted,
                    "Too many incorrect attempts. Please request a new code.");
            }

            _logger.LogError(ex,
                "Twilio Verify check failed for {Phone}. Code={Code} Status={Status} Message={Message}.",
                MaskPhone(normalized), ex.Code, ex.Status, ex.Message);
            return Result<VerifyOtpResult>.Failure(
                ErrorCodes.PhoneVerificationFailed,
                "We could not verify the code. Please try again.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected Twilio Verify check failure for {Phone}.", MaskPhone(normalized));
            return Result<VerifyOtpResult>.Failure(
                ErrorCodes.PhoneVerificationFailed,
                "We could not verify the code. Please try again.");
        }
    }

    private bool TryGetReadyClient(out global::Twilio.Clients.ITwilioRestClient client, out Result? error)
    {
        client = null!;

        if (!_clientProvider.IsConfigured)
        {
            error = Result.Failure(ErrorCodes.ProviderNotConfigured, "Phone verification is not configured.");
            return false;
        }

        if (!_settings.Verify.HasService())
        {
            error = Result.Failure(ErrorCodes.ProviderNotConfigured, "Phone verification is not configured.");
            return false;
        }

        var resolved = _clientProvider.GetClient();
        if (resolved is null)
        {
            error = Result.Failure(ErrorCodes.ProviderNotConfigured, "Phone verification is not configured.");
            return false;
        }

        client = resolved;
        error = null;
        return true;
    }

    /// <summary>
    /// Logs only the country code + last 4 digits so server logs do not
    /// retain full phone numbers. Returns "***" for anything unrecognisable.
    /// </summary>
    private static string MaskPhone(string e164)
    {
        if (string.IsNullOrEmpty(e164) || e164.Length < 6) return "***";
        var tail = e164[^4..];
        return e164.Length > 7 ? e164[..3] + "***" + tail : "***" + tail;
    }
}
