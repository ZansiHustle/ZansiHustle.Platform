using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Auth;
using ZansiHustle.Application.Communications.Email.Interfaces;
using ZansiHustle.Application.Communications.Otp.Interfaces;
using ZansiHustle.Application.Communications.Otp.Models;
using ZansiHustle.Application.Communications.Sms.Interfaces;
using ZansiHustle.Application.Communications.TestMode;
using ZansiHustle.Application.Communications.WhatsApp.Interfaces;
using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Otp;

/// <summary>
/// Channel-agnostic OTP orchestrator. Generates cryptographically random
/// codes, persists hashed sessions, enforces cooldown + attempt policies,
/// and dispatches via the requested channel provider.
/// </summary>
public sealed class OtpService : IOtpService
{
    private readonly IOtpStore _store;
    private readonly OtpSettings _settings;
    private readonly AuthTestModeSettings _testMode;
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IEmailService _emailService;
    private readonly ICommunicationRecipientResolver _recipients;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IOtpStore store,
        IOptions<OtpSettings> settings,
        IOptions<AuthTestModeSettings> testModeSettings,
        ISmsService smsService,
        IWhatsAppService whatsAppService,
        IEmailService emailService,
        ICommunicationRecipientResolver recipients,
        ILogger<OtpService> logger)
    {
        _store = store;
        _settings = settings.Value ?? new OtpSettings();
        _testMode = testModeSettings.Value ?? new AuthTestModeSettings();
        _smsService = smsService;
        _whatsAppService = whatsAppService;
        _emailService = emailService;
        _recipients = recipients;
        _logger = logger;
    }

    public async Task<Result<OtpIssueResult>> IssueAsync(OtpIssueRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Destination))
            return Result<OtpIssueResult>.Failure(ErrorCodes.BadRequest, "Destination is required.");

        var destination = request.Destination.Trim();

        // Enforce resend cooldown
        var existing = await _store.FindActiveByDestinationAsync(destination, request.Purpose, request.Channel, cancellationToken);
        if (existing is not null && !existing.IsConsumed && existing.ExpiresAtUtc > DateTime.UtcNow)
        {
            var sinceIssued = DateTime.UtcNow - existing.CreatedOnUtc;
            if (sinceIssued.TotalSeconds < _settings.ResendCooldownSeconds)
            {
                var wait = _settings.ResendCooldownSeconds - (int)sinceIssued.TotalSeconds;
                return Result<OtpIssueResult>.Failure(
                    ErrorCodes.OtpResendCooldown,
                    $"Please wait {wait} seconds before requesting another code.");
            }

            // Invalidate the previous outstanding code so only the newest is valid.
            await _store.DeleteAsync(existing.SessionId, cancellationToken);
        }

        var code = GenerateNumericCode(_settings.CodeLength);
        var record = new OtpSessionRecord
        {
            SessionId = Guid.NewGuid().ToString("N"),
            Destination = destination,
            Purpose = request.Purpose,
            Channel = request.Channel,
            CodeHash = HashCode(code, destination),
            CreatedOnUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(_settings.TtlSeconds),
            AttemptCount = 0,
            IsConsumed = false,
            UserId = request.UserId
        };

        await _store.SaveAsync(record, cancellationToken);

        var dispatch = await DispatchAsync(request, code, cancellationToken);
        if (!dispatch.IsSuccess)
        {
            await _store.DeleteAsync(record.SessionId, cancellationToken);
            return Result<OtpIssueResult>.Failure(dispatch.Code, dispatch.Message);
        }

        _logger.LogInformation(
            "OTP issued. SessionId={SessionId} Purpose={Purpose} Channel={Channel}.",
            record.SessionId, request.Purpose, request.Channel);

        return Result<OtpIssueResult>.Success(new OtpIssueResult
        {
            SessionId = record.SessionId,
            ExpiresAtUtc = record.ExpiresAtUtc,
            CodeLength = _settings.CodeLength,
            ResendCooldownSeconds = _settings.ResendCooldownSeconds
        }, "Verification code sent.");
    }

    public async Task<Result<OtpVerifiedContext>> VerifyAsync(OtpVerifyRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
            return Result<OtpVerifiedContext>.Failure(ErrorCodes.BadRequest, "Session id is required.");
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result<OtpVerifiedContext>.Failure(ErrorCodes.BadRequest, "Verification code is required.");

        var record = await _store.FindAsync(request.SessionId, cancellationToken);
        if (record is null || record.IsConsumed)
            return Result<OtpVerifiedContext>.Failure(ErrorCodes.OtpInvalid, "Invalid verification code.");

        if (record.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await _store.DeleteAsync(record.SessionId, cancellationToken);
            return Result<OtpVerifiedContext>.Failure(ErrorCodes.OtpExpired, "The verification code has expired. Please request a new one.");
        }

        if (record.AttemptCount >= _settings.MaxAttempts)
        {
            await _store.DeleteAsync(record.SessionId, cancellationToken);
            return Result<OtpVerifiedContext>.Failure(ErrorCodes.OtpExhausted, "Too many incorrect attempts. Please request a new code.");
        }

        // ── Test-mode bypass ───────────────────────────────────────────
        // Accept the configured bypass code (default "111111") iff
        // Auth:TestMode:Enabled is true. Session must still exist, be
        // unconsumed, unexpired, and within attempts — see guards
        // above — so a bypass cannot manufacture an OtpVerifiedContext
        // out of thin air. The real hashed-compare branch below is
        // untouched. Never log the submitted code.
        if (_testMode.Enabled
            && !string.IsNullOrEmpty(_testMode.BypassCode)
            && request.Code.Trim() == _testMode.BypassCode)
        {
            record.IsConsumed = true;
            await _store.UpdateAsync(record, cancellationToken);
            _logger.LogWarning(
                "[TEST_MODE] OTP bypass accepted (session) for SessionId={SessionId} Channel={Channel} Purpose={Purpose}.",
                record.SessionId, record.Channel, record.Purpose);
            return Result<OtpVerifiedContext>.Success(new OtpVerifiedContext
            {
                Destination = record.Destination,
                UserId = record.UserId,
                Purpose = record.Purpose,
                Channel = record.Channel
            }, "Verification successful.");
        }

        var submittedHash = HashCode(request.Code.Trim(), record.Destination);
        if (!FixedTimeEquals(submittedHash, record.CodeHash))
        {
            record.AttemptCount += 1;
            await _store.UpdateAsync(record, cancellationToken);
            var remaining = Math.Max(0, _settings.MaxAttempts - record.AttemptCount);
            return Result<OtpVerifiedContext>.Failure(
                ErrorCodes.OtpInvalid,
                remaining > 0
                    ? $"Invalid code. {remaining} attempt(s) remaining."
                    : "Invalid code. Please request a new one.");
        }

        record.IsConsumed = true;
        await _store.UpdateAsync(record, cancellationToken);

        return Result<OtpVerifiedContext>.Success(new OtpVerifiedContext
        {
            Destination = record.Destination,
            UserId = record.UserId,
            Purpose = record.Purpose,
            Channel = record.Channel
        }, "Verification successful.");
    }

    private async Task<Result> DispatchAsync(OtpIssueRequest request, string code, CancellationToken cancellationToken)
    {
        switch (request.Channel)
        {
            case OtpChannel.Sms:
            {
                var send = await _smsService.SendOtpAsync(request.Destination, code, cancellationToken);
                return send.IsSuccess ? Result.Success() : Result.Failure(send.Code, send.Message);
            }
            case OtpChannel.WhatsApp:
            {
                var send = await _whatsAppService.SendOtpAsync(request.Destination, code, cancellationToken);
                return send.IsSuccess ? Result.Success() : Result.Failure(send.Code, send.Message);
            }
            case OtpChannel.Email:
            {
                var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? "there" : request.DisplayName!;
                var ttlMinutes = (int)Math.Ceiling(_settings.TtlSeconds / 60.0);
                // UAT/test override: redirect DELIVERY to the configured test
                // email while keeping the session's real destination (used for
                // hashing + verify) intact — so the override never breaks the
                // code check. Default no-op (only overrides when
                // CommunicationTestMode + OverrideSecurityOtpRecipients are on).
                var deliverTo = _recipients.ResolveEmail(request.Destination, CommunicationPurpose.SecurityOtp)
                                ?? request.Destination;
                // Pick purpose-specific email copy so the user sees language that
                // matches why they're receiving the code.
                if (request.Purpose == OtpPurpose.PasswordReset)
                    return await _emailService.SendPasswordResetOtpAsync(deliverTo, displayName, code, ttlMinutes, cancellationToken);
                if (request.Purpose == OtpPurpose.EmailVerification)
                    return await _emailService.SendAccountVerificationOtpAsync(deliverTo, displayName, code, ttlMinutes, cancellationToken);
                return await _emailService.SendOtpAsync(deliverTo, displayName, code, cancellationToken);
            }
            default:
                return Result.Failure(ErrorCodes.BadRequest, "Unsupported OTP channel.");
        }
    }

    private static string GenerateNumericCode(int length)
    {
        if (length < 4) length = 4;
        if (length > 10) length = 10;

        Span<byte> buffer = stackalloc byte[length];
        RandomNumberGenerator.Fill(buffer);

        var sb = new StringBuilder(length);
        foreach (var b in buffer)
        {
            sb.Append((char)('0' + (b % 10)));
        }
        return sb.ToString();
    }

    private static string HashCode(string code, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes($"{salt}|{code}");
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
