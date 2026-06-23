using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ZansiHustle.Application.Auth.Dtos;
using ZansiHustle.Application.Communications.Email.Interfaces;
using ZansiHustle.Application.Communications.Otp.Interfaces;
using ZansiHustle.Application.Communications.Otp.Models;
using ZansiHustle.Application.Communications.PhoneVerification;
using ZansiHustle.Application.Persistence.Identity;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Auth;

/// <summary>
/// Handles user authentication, account registration, token refresh, email verification,
/// password recovery, and logout operations.
/// </summary>
public sealed class AuthService : IAuthService
{
    // Cache key prefix for phone-reset sessions. Keeps the in-process map
    // namespaced so it can co-exist with other IMemoryCache consumers.
    private const string PhoneResetSessionPrefix = "auth:phone-reset-session:";
    // 10-minute TTL aligns roughly with Twilio Verify's own 10-minute code
    // expiry. Past this window the verify call would fail anyway, so the
    // session is useless.
    private static readonly TimeSpan PhoneResetSessionTtl = TimeSpan.FromMinutes(10);

    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly IOtpService _otpService;
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthService> _logger;
    private readonly ZansiHustle.Application.Users.IUserProfileService _userProfileService;

    public AuthService(
        UserManager<User> userManager,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        IOtpService otpService,
        IPhoneVerificationService phoneVerificationService,
        IMemoryCache cache,
        ILogger<AuthService> logger,
        ZansiHustle.Application.Users.IUserProfileService userProfileService)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _otpService = otpService;
        _phoneVerificationService = phoneVerificationService;
        _cache = cache;
        _logger = logger;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Server-side bookkeeping for a phone-channel reset OTP. Twilio Verify
    /// itself stores no per-flow state we can correlate to a user, so we
    /// keep the (sessionId → user, phone) mapping in-process for the
    /// lifetime of the OTP.
    /// </summary>
    private sealed class PhoneResetSession
    {
        public Guid UserId { get; init; }
        public string PhoneNumber { get; init; } = string.Empty;
        public MobileOtpChannel Channel { get; init; }
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokenDto>> LoginAsync(LoginDto dto)
    {
        try
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
                return Result<AuthTokenDto>.Failure(ErrorCodes.InvalidCredentials, "Invalid email or password.");

            if (!user.IsActive || user.AccountStatus != AccountStatus.Active)
                return Result<AuthTokenDto>.Failure(ErrorCodes.InactiveAccount, "Your account is not active. Please contact support.");

            var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!validPassword)
                return Result<AuthTokenDto>.Failure(ErrorCodes.InvalidCredentials, "Invalid email or password.");

            // ── Account verification gate (phone OR email) ──────────────────
            // Verification policy: an account is verified when EITHER its phone
            // OR its email is confirmed. Login is blocked ONLY when NEITHER is —
            // so a user who verified by phone, OR by email, passes.
            //   • Existing accounts were backfilled to PhoneNumberConfirmed=true
            //     (migration BackfillPhoneNumberConfirmed) → never blocked.
            //   • A phone-less account must confirm its email; a phone+email
            //     account can use either channel.
            // We don't issue tokens — we return RequiresVerification + the phone
            // AND email so the client can offer SMS/WhatsApp/Email and resend.
            var isVerified = user.PhoneNumberConfirmed || user.EmailConfirmed;
            if (!isVerified)
            {
                _logger.LogInformation(
                    "Login requires verification (no session issued). UserId={UserId}", user.Id);
                return Result<AuthTokenDto>.Success(new AuthTokenDto
                {
                    RequiresVerification = true,
                    VerificationPhoneNumber = user.PhoneNumber,
                    VerificationEmail = user.Email,
                    UserId = user.Id,
                }, "Please verify your account to continue.");
            }

            // v1: email verification is a soft signal (surfaced via CurrentUserDto.EmailConfirmed).
            // Sensitive flows (seller activation, payouts, KYC) gate on EmailConfirmed in feature code.

            var token = await _jwtTokenGenerator.GenerateTokenAsync(user);

            _logger.LogInformation("User {UserId} logged in successfully.", user.Id);
            return Result<AuthTokenDto>.Success(token, "Login successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email {Email}.", dto.Email);
            return Result<AuthTokenDto>.Failure(ErrorCodes.Exception, "Login failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> RegisterAsync(RegisterDto dto)
    {
        try
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
                return Result<Guid>.Failure(ErrorCodes.EmailTaken, "An account with this email already exists.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                IsActive = true,
                AccountStatus = AccountStatus.Active,
                CreatedOnUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                var isPasswordIssue = createResult.Errors.Any(e =>
                    e.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase));
                var code = isPasswordIssue ? ErrorCodes.WeakPassword : ErrorCodes.BadRequest;
                return Result<Guid>.Failure(code, errors);
            }

            var roleResult = await _userManager.AddToRolesAsync(user, dto.UserRoles.Select(x=>x.ToString()));
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                return Result<Guid>.Failure(ErrorCodes.BadRequest, $"Role assignment failed: {errors}");
            }

            _logger.LogInformation("User registered successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
            return Result<Guid>.Success(user.Id, "Registration successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email {Email}.", dto.Email);
            return Result<Guid>.Failure(ErrorCodes.Exception, "Registration failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<CheckAvailabilityResponseDto>> CheckAvailabilityAsync(CheckAvailabilityRequestDto dto)
    {
        try
        {
            if (dto is null)
                return Result<CheckAvailabilityResponseDto>.Failure(
                    ErrorCodes.BadRequest, "Request is required.");

            var hasEmail = !string.IsNullOrWhiteSpace(dto.Email);
            var hasPhone = !string.IsNullOrWhiteSpace(dto.PhoneNumber);
            if (!hasEmail && !hasPhone)
                return Result<CheckAvailabilityResponseDto>.Failure(
                    ErrorCodes.BadRequest,
                    "Provide an email or phone number to check.");

            // Default to "available" for any field the caller omitted —
            // the wizard's Step 1 always sends both, but the API stays
            // useful for callers that only want one half.
            var response = new CheckAvailabilityResponseDto
            {
                EmailAvailable = true,
                PhoneAvailable = true,
            };

            if (hasEmail)
            {
                var email = dto.Email!.Trim().ToLowerInvariant();
                var existing = await _userManager.FindByEmailAsync(email);
                response.EmailAvailable = existing is null;
            }

            if (hasPhone)
            {
                // Match against both the raw value the caller sent and the
                // SA-normalised E.164 form, so historical accounts with
                // looser phone formats still register as taken. Mirrors
                // the lookup in RequestPasswordResetOtpAsync.
                var phoneRaw = dto.PhoneNumber!.Trim();
                var phoneNorm = NormalisePhone(phoneRaw);
                var existing = await _userManager.Users.FirstOrDefaultAsync(u =>
                    u.PhoneNumber == phoneRaw || u.PhoneNumber == phoneNorm);
                response.PhoneAvailable = existing is null;
            }

            return Result<CheckAvailabilityResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Availability check failed.");
            return Result<CheckAvailabilityResponseDto>.Failure(
                ErrorCodes.Exception, "Could not check availability.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokenDto>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokens = await _jwtTokenGenerator.RefreshTokenAsync(refreshToken);
            if (tokens == null)
                return Result<AuthTokenDto>.Failure(ErrorCodes.InvalidRefreshToken, "Your session has expired. Please sign in again.");

            return Result<AuthTokenDto>.Success(tokens, "Token refreshed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token flow failed.");
            return Result<AuthTokenDto>.Failure(ErrorCodes.Exception, "Failed to refresh token.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SendEmailVerificationAsync(string email, string callbackBaseUrl)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());
            if (user == null)
                return Result.Success("If the account exists, a verification email has been sent.");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = $"{callbackBaseUrl}?userId={user.Id}&token={Uri.EscapeDataString(token)}";
            var sendResult = await _emailService.SendVerifyEmailAsync(user.Email!, user.FirstName, callbackUrl);

            if (!sendResult.IsSuccess)
            {
                _logger.LogWarning("Failed to send verification email to {Email}. Reason: {Message}", email, sendResult.Message);
                return sendResult;
            }

            _logger.LogInformation("Email verification link sent to {Email}.", email);
            var message = new
            {
                response = "Verification email sent.",
                callBackUrl = callbackBaseUrl,
                token
            };
            string jsonString = JsonConvert.SerializeObject(message);
            return Result.Success(jsonString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}.", email);
            return Result.Failure(ErrorCodes.EmailSendFailed, "Failed to send verification email.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> VerifyEmailAsync(string userId, string token)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure(ErrorCodes.NotFound, "User not found.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                return Result.Failure(ErrorCodes.InvalidResetToken, $"Email verification failed. The link may have expired. {errors}");
            }

            var sendResult = await _emailService.SendEmailVerifiedConfirmationAsync(user.Email!, user.FirstName);
            if (!sendResult.IsSuccess)
                _logger.LogWarning("Email verified for user {UserId}, but confirmation email failed. Reason: {Message}", user.Id, sendResult.Message);

            return Result.Success("Email verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed for user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Email verification failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ForgotPasswordAsync(string email, string callbackBaseUrl)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());
            if (user == null)
                return Result.Success("If the account exists, a password reset email has been sent.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = $"{callbackBaseUrl}?userId={user.Id}&token={Uri.EscapeDataString(token)}";
            var sendResult = await _emailService.SendPasswordResetAsync(user.Email!, user.FirstName, callbackUrl);

            if (!sendResult.IsSuccess)
            {
                _logger.LogWarning("Failed to send password reset email to {Email}. Reason: {Message}", email, sendResult.Message);
                return sendResult;
            }

            _logger.LogInformation("Password reset email sent to {Email}.", email);
            return Result.Success("Password reset email sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}.", email);
            return Result.Failure(ErrorCodes.EmailSendFailed, "Failed to send password reset email.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<ForgotPasswordResponseDto>> RequestPasswordResetOtpAsync(ForgotPasswordRequestDto dto)
    {
        // Normalise inputs early so later lookups/logging are predictable.
        var identifier = (dto.EmailOrPhone ?? string.Empty).Trim();
        var channel = ParseChannel(dto.Channel);

        if (string.IsNullOrWhiteSpace(identifier))
            return Result<ForgotPasswordResponseDto>.Failure(ErrorCodes.BadRequest,
                IsPhoneChannel(channel) ? "Phone number is required." : "Email address is required.");

        _logger.LogInformation(
            "Password reset OTP requested. Channel={Channel} IdentifierPreview={Preview}",
            channel, MaskIdentifier(identifier, channel));

        // ── User lookup ─────────────────────────────────────────────
        // Try to find the user by the channel-appropriate identifier.
        // If the identifier doesn't resolve to a user we STILL return
        // the same response shape the caller gets on success (with a
        // throwaway session id) — this makes the endpoint
        // enumeration-safe. An attacker cannot distinguish "email not
        // on file" from "email on file".
        //
        // DB connectivity failures get their own explicit error so the
        // caller sees a helpful message rather than a generic 500.
        User? user;
        try
        {
            user = IsPhoneChannel(channel)
                ? await _userManager.Users.FirstOrDefaultAsync(u =>
                      u.PhoneNumber == identifier || u.PhoneNumber == NormalisePhone(identifier))
                : await _userManager.FindByEmailAsync(identifier.ToLowerInvariant());
        }
        catch (Exception dbEx)
        {
            _logger.LogError(dbEx, "Password reset: user lookup failed — DB unreachable?");
            return Result<ForgotPasswordResponseDto>.Failure(
                ErrorCodes.Exception,
                "The password reset service is temporarily unavailable. Please try again in a moment.");
        }

        if (user is null)
        {
            // Enumeration-safe response. Fabricate a plausible session
            // envelope so the client can proceed to the verify screen;
            // verification will naturally fail because no record exists.
            _logger.LogInformation("Password reset: no user for identifier, returning dummy session.");
            return Result<ForgotPasswordResponseDto>.Success(
                BuildDummySession(channel, identifier),
                "If an account matches, a verification code has been sent.");
        }

        if (!user.IsActive || user.AccountStatus != AccountStatus.Active)
        {
            // Same enumeration-safe shape — don't advertise "your
            // account is inactive" to attackers probing the endpoint.
            _logger.LogInformation("Password reset: user {UserId} is inactive, returning dummy session.", user.Id);
            return Result<ForgotPasswordResponseDto>.Success(
                BuildDummySession(channel, identifier),
                "If an account matches, a verification code has been sent.");
        }

        // Phone channels require a phone on file; email requires an email.
        // We refuse rather than silently succeed — but only after the user
        // lookup succeeded, so this branch can't be used for enumeration.
        var destination = IsPhoneChannel(channel)
            ? NormalisePhone(user.PhoneNumber ?? string.Empty)
            : user.Email ?? identifier.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(destination))
        {
            return Result<ForgotPasswordResponseDto>.Failure(
                ErrorCodes.BadRequest,
                IsPhoneChannel(channel)
                    ? "No phone number on file for this account. Please reset via email."
                    : "No email on file for this account.");
        }

        // ── Issue the OTP ───────────────────────────────────────────
        // Phone channels go through Twilio Verify (no plaintext code is
        // ever stored on our side); email goes through the existing
        // OtpService which handles its own template/code lifecycle.
        if (IsPhoneChannel(channel))
        {
            var mobileChannel = ToMobileChannel(channel);
            var send = await _phoneVerificationService.SendOtpAsync(destination, mobileChannel);
            if (!send.IsSuccess || send.Data is null)
            {
                _logger.LogWarning(
                    "Password reset OTP dispatch failed (phone) for user {UserId}. Code={Code} Message={Message}",
                    user.Id, send.Code, send.Message);
                return Result<ForgotPasswordResponseDto>.Failure(send.Code, send.Message);
            }

            // Cache (sessionId → user, normalised phone, channel) so the
            // verify step can look up the user without trusting the client
            // to round-trip the phone number. TTL matches Twilio's 10-min
            // code lifetime.
            var sessionId = Guid.NewGuid().ToString("N");
            _cache.Set(
                PhoneResetSessionPrefix + sessionId,
                new PhoneResetSession
                {
                    UserId = user.Id,
                    PhoneNumber = send.Data.PhoneNumber,
                    Channel = mobileChannel
                },
                PhoneResetSessionTtl);

            _logger.LogInformation(
                "Password reset OTP issued via Twilio Verify. UserId={UserId} SessionId={SessionId} Channel={Channel}",
                user.Id, sessionId, mobileChannel);

            return Result<ForgotPasswordResponseDto>.Success(new ForgotPasswordResponseDto
            {
                SessionId = sessionId,
                ExpiresAtUtc = DateTime.UtcNow.Add(PhoneResetSessionTtl),
                CodeLength = 6, // Twilio Verify default; presentational only.
                ResendCooldownSeconds = send.Data.ResendCooldownSeconds,
                Channel = ChannelWire(channel),
                DestinationMasked = MaskIdentifier(destination, channel)
            }, "Verification code sent.");
        }

        // Email path — unchanged from before.
        var issueRequest = new OtpIssueRequest
        {
            Destination = destination,
            Channel = channel,
            Purpose = OtpPurpose.PasswordReset,
            UserId = user.Id,
            DisplayName = user.FirstName ?? user.Email ?? "there"
        };

        var issue = await _otpService.IssueAsync(issueRequest);
        if (!issue.IsSuccess || issue.Data is null)
        {
            _logger.LogWarning(
                "Password reset OTP dispatch failed for user {UserId}. Code={Code} Message={Message}",
                user.Id, issue.Code, issue.Message);
            return Result<ForgotPasswordResponseDto>.Failure(issue.Code, issue.Message);
        }

        _logger.LogInformation(
            "Password reset OTP issued for user {UserId}. SessionId={SessionId}",
            user.Id, issue.Data.SessionId);

        return Result<ForgotPasswordResponseDto>.Success(new ForgotPasswordResponseDto
        {
            SessionId = issue.Data.SessionId,
            ExpiresAtUtc = issue.Data.ExpiresAtUtc,
            CodeLength = issue.Data.CodeLength,
            ResendCooldownSeconds = issue.Data.ResendCooldownSeconds,
            Channel = ChannelWire(channel),
            DestinationMasked = MaskIdentifier(destination, channel)
        }, "Verification code sent.");
    }

    /// <inheritdoc />
    public async Task<Result<VerifyResetOtpResponseDto>> VerifyPasswordResetOtpAsync(VerifyResetOtpRequestDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.SessionId) || string.IsNullOrWhiteSpace(dto.Code))
            return Result<VerifyResetOtpResponseDto>.Failure(ErrorCodes.BadRequest, "Session id and code are required.");

        // Phone-channel sessions live in IMemoryCache (set by the SMS /
        // WhatsApp branch of RequestPasswordResetOtpAsync). Try that first;
        // a hit means we delegate the code check to Twilio Verify and mint
        // a reset token if approved. A miss falls through to the legacy
        // email path which uses the custom OtpService.
        var phoneCacheKey = PhoneResetSessionPrefix + dto.SessionId;
        if (_cache.TryGetValue<PhoneResetSession>(phoneCacheKey, out var phoneSession) && phoneSession is not null)
        {
            var twilio = await _phoneVerificationService.VerifyOtpAsync(phoneSession.PhoneNumber, dto.Code);
            if (!twilio.IsSuccess || twilio.Data is null || !twilio.Data.Verified)
            {
                // Don't drop the cache entry on failure — Twilio Verify
                // tracks its own attempt cap and will start returning
                // OTP_EXHAUSTED once the user has burned all retries.
                return Result<VerifyResetOtpResponseDto>.Failure(twilio.Code, twilio.Message);
            }

            // Single-use: invalidate the session immediately so the same
            // sessionId+code can't be replayed against /verify-reset-otp.
            _cache.Remove(phoneCacheKey);

            User? phoneUser;
            try
            {
                phoneUser = await _userManager.FindByIdAsync(phoneSession.UserId.ToString());
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Password reset verify (phone): user lookup failed.");
                return Result<VerifyResetOtpResponseDto>.Failure(
                    ErrorCodes.Exception,
                    "The password reset service is temporarily unavailable. Please try again in a moment.");
            }

            if (phoneUser is null)
                return Result<VerifyResetOtpResponseDto>.Failure(ErrorCodes.NotFound, "Account not found.");

            var phoneResetToken = await _userManager.GeneratePasswordResetTokenAsync(phoneUser);
            _logger.LogInformation(
                "Password reset OTP verified via Twilio Verify for user {UserId}.", phoneUser.Id);

            return Result<VerifyResetOtpResponseDto>.Success(new VerifyResetOtpResponseDto
            {
                UserId = phoneUser.Id.ToString(),
                ResetToken = phoneResetToken
            }, "Verification successful.");
        }

        // Email path — the OtpService handles TTL / attempt caps / one-use semantics.
        var verify = await _otpService.VerifyAsync(new OtpVerifyRequest
        {
            SessionId = dto.SessionId,
            Code = dto.Code
        });

        if (!verify.IsSuccess || verify.Data is null)
            return Result<VerifyResetOtpResponseDto>.Failure(verify.Code, verify.Message);

        if (verify.Data.Purpose != OtpPurpose.PasswordReset)
            // Guard against cross-purpose replay: a code issued for
            // phone-verification must not unlock password reset.
            return Result<VerifyResetOtpResponseDto>.Failure(ErrorCodes.OtpInvalid, "Invalid verification code.");

        if (!verify.Data.UserId.HasValue)
            // Shouldn't happen — RequestPasswordResetOtpAsync always
            // sets UserId on the OTP session. Defensive fail-close.
            return Result<VerifyResetOtpResponseDto>.Failure(ErrorCodes.OtpInvalid, "Invalid verification code.");

        User? user;
        try
        {
            user = await _userManager.FindByIdAsync(verify.Data.UserId.Value.ToString());
        }
        catch (Exception dbEx)
        {
            _logger.LogError(dbEx, "Password reset verify: user lookup failed — DB unreachable?");
            return Result<VerifyResetOtpResponseDto>.Failure(
                ErrorCodes.Exception,
                "The password reset service is temporarily unavailable. Please try again in a moment.");
        }

        if (user is null)
            return Result<VerifyResetOtpResponseDto>.Failure(ErrorCodes.NotFound, "Account not found.");

        // Mint an Identity password-reset token that the client passes
        // into the existing /reset-password endpoint. Token is
        // time-bound and single-use via the Identity token provider.
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        _logger.LogInformation("Password reset OTP verified for user {UserId}.", user.Id);

        return Result<VerifyResetOtpResponseDto>.Success(new VerifyResetOtpResponseDto
        {
            UserId = user.Id.ToString(),
            ResetToken = resetToken
        }, "Verification successful.");
    }

    // ── Password-reset helpers ─────────────────────────────────────

    private static OtpChannel ParseChannel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return OtpChannel.Email;
        return raw.Trim().ToLowerInvariant() switch
        {
            "sms"      => OtpChannel.Sms,
            "whatsapp" => OtpChannel.WhatsApp,
            "email"    => OtpChannel.Email,
            _          => OtpChannel.Email
        };
    }

    /// <summary>
    /// True for channels that go through the Twilio Verify provider
    /// (phone-based). Email lives on a separate code path with its own
    /// custom OTP store and templates.
    /// </summary>
    private static bool IsPhoneChannel(OtpChannel channel)
        => channel == OtpChannel.Sms || channel == OtpChannel.WhatsApp;

    private static MobileOtpChannel ToMobileChannel(OtpChannel channel) => channel switch
    {
        OtpChannel.Sms => MobileOtpChannel.Sms,
        OtpChannel.WhatsApp => MobileOtpChannel.WhatsApp,
        _ => MobileOtpChannel.Sms
    };

    private static string ChannelWire(OtpChannel channel) => channel switch
    {
        OtpChannel.Sms => "sms",
        OtpChannel.WhatsApp => "whatsapp",
        _ => "email"
    };

    // Very small phone normaliser — enough to match either the stored
    // "+27821234567" or the looser variants users type ("0821234567",
    // "+27 82 123 4567"). Full libphonenumber parsing lives elsewhere
    // in the SMS provider layer.
    private static string NormalisePhone(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var compact = new string(raw.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (compact.StartsWith('+')) return compact;
        if (compact.StartsWith("27")) return "+" + compact;
        if (compact.StartsWith('0') && compact.Length >= 10) return "+27" + compact.Substring(1);
        return compact;
    }

    private static string MaskIdentifier(string identifier, OtpChannel channel)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return "";
        if (IsPhoneChannel(channel))
        {
            // Show last 3 digits: "+27** *** *123"
            var digits = new string(identifier.Where(char.IsDigit).ToArray());
            if (digits.Length <= 3) return new string('*', digits.Length);
            return "+" + new string('*', digits.Length - 3) + digits[^3..];
        }
        // Email masking: first char + *** + @domain
        var at = identifier.IndexOf('@');
        if (at <= 1) return identifier;
        var local = identifier[..at];
        var domain = identifier[at..];
        return local[0] + new string('*', Math.Max(1, local.Length - 1)) + domain;
    }

    // When the identifier doesn't resolve to a user (or the account is
    // inactive), we return a response that LOOKS like a real issuance
    // to prevent account enumeration. The session id is random and
    // will simply fail to verify — which is the correct UX for
    // "attacker probed an address that isn't on file".
    private static ForgotPasswordResponseDto BuildDummySession(OtpChannel channel, string identifier)
    {
        return new ForgotPasswordResponseDto
        {
            SessionId = Guid.NewGuid().ToString("N"),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            CodeLength = 6,
            ResendCooldownSeconds = 30,
            Channel = ChannelWire(channel),
            DestinationMasked = MaskIdentifier(identifier, channel)
        };
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure(ErrorCodes.NotFound, "User not found.");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                var isPasswordIssue = result.Errors.Any(e =>
                    e.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase));
                var isTokenIssue = result.Errors.Any(e =>
                    e.Code.Contains("Token", StringComparison.OrdinalIgnoreCase));
                var code = isPasswordIssue
                    ? ErrorCodes.WeakPassword
                    : (isTokenIssue ? ErrorCodes.InvalidResetToken : ErrorCodes.BadRequest);
                return Result.Failure(code, errors);
            }

            await _jwtTokenGenerator.RevokeAllRefreshTokensForUserAsync(user.Id);
            return Result.Success("Password reset successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset failed for user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Password reset failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure(ErrorCodes.NotFound, "User not found.");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                var isPasswordIssue = result.Errors.Any(e =>
                    e.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase));
                var isWrongCurrent = result.Errors.Any(e =>
                    string.Equals(e.Code, "PasswordMismatch", StringComparison.OrdinalIgnoreCase));
                var code = isWrongCurrent
                    ? ErrorCodes.InvalidCredentials
                    : (isPasswordIssue ? ErrorCodes.WeakPassword : ErrorCodes.BadRequest);
                return Result.Failure(code, errors);
            }

            await _jwtTokenGenerator.RevokeAllRefreshTokensForUserAsync(user.Id);
            return Result.Success("Password changed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password change failed for user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Password change failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.IsActive || user.AccountStatus != AccountStatus.Active)
                return Result<CurrentUserDto>.Failure(ErrorCodes.NotFound, "User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            // Surface the user's profile picture (from their UserProfile) so the
            // mobile side menu + profile screen can show it. Best-effort: a
            // missing/failed profile lookup just leaves the avatar to fall back.
            string? profileImageUrl = null;
            try
            {
                var profile = await _userProfileService.GetByUserIdAsync(user.Id);
                if (profile.IsSuccess)
                    profileImageUrl = profile.Data?.ProfileImageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load profile image for user {UserId}.", user.Id);
            }

            var dto = new CurrentUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                AccountStatus = user.AccountStatus.ToString(),
                ProfileImageUrl = profileImageUrl
            };

            return Result<CurrentUserDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load current user {UserId}.", userId);
            return Result<CurrentUserDto>.Failure(ErrorCodes.Exception, "Failed to load current user.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<EmailOtpSessionDto>> RequestAccountEmailOtpAsync(string email)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return Result<EmailOtpSessionDto>.Failure(ErrorCodes.BadRequest, "Email address is required.");

        User? user;
        try
        {
            user = await _userManager.FindByEmailAsync(normalizedEmail);
        }
        catch (Exception dbEx)
        {
            _logger.LogError(dbEx, "Account email OTP: user lookup failed — DB unreachable?");
            return Result<EmailOtpSessionDto>.Failure(
                ErrorCodes.Exception,
                "The verification service is temporarily unavailable. Please try again in a moment.");
        }

        // Enumeration-safe: an unknown / inactive email gets the same envelope
        // shape with a throwaway session that simply won't verify.
        if (user is null || !user.IsActive || user.AccountStatus != AccountStatus.Active)
        {
            _logger.LogInformation("Account email OTP: no active user for email, returning dummy session.");
            return Result<EmailOtpSessionDto>.Success(new EmailOtpSessionDto
            {
                SessionId = Guid.NewGuid().ToString("N"),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                CodeLength = 6,
                ResendCooldownSeconds = 30,
                EmailMasked = MaskIdentifier(normalizedEmail, OtpChannel.Email),
            }, "If an account matches, a verification code has been sent.");
        }

        var issue = await _otpService.IssueAsync(new OtpIssueRequest
        {
            Destination = user.Email!,
            Channel = OtpChannel.Email,
            Purpose = OtpPurpose.EmailVerification,
            UserId = user.Id,
            DisplayName = user.FirstName ?? user.Email ?? "there",
        });
        if (!issue.IsSuccess || issue.Data is null)
        {
            _logger.LogWarning(
                "Account email OTP dispatch failed for user {UserId}. Code={Code} Message={Message}",
                user.Id, issue.Code, issue.Message);
            return Result<EmailOtpSessionDto>.Failure(issue.Code, issue.Message);
        }

        _logger.LogInformation(
            "Account email OTP issued for user {UserId}. SessionId={SessionId}", user.Id, issue.Data.SessionId);

        return Result<EmailOtpSessionDto>.Success(new EmailOtpSessionDto
        {
            SessionId = issue.Data.SessionId,
            ExpiresAtUtc = issue.Data.ExpiresAtUtc,
            CodeLength = issue.Data.CodeLength,
            ResendCooldownSeconds = issue.Data.ResendCooldownSeconds,
            EmailMasked = MaskIdentifier(user.Email!, OtpChannel.Email),
        }, "Verification code sent.");
    }

    /// <inheritdoc />
    public async Task<Result<VerifyEmailOtpResponseDto>> VerifyAccountEmailOtpAsync(string sessionId, string code)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(code))
            return Result<VerifyEmailOtpResponseDto>.Failure(ErrorCodes.BadRequest, "Session id and code are required.");

        var verify = await _otpService.VerifyAsync(new OtpVerifyRequest { SessionId = sessionId, Code = code });
        if (!verify.IsSuccess || verify.Data is null)
            return Result<VerifyEmailOtpResponseDto>.Failure(verify.Code, verify.Message);

        // Cross-purpose guard: only an EmailVerification code may confirm email
        // (a password-reset code must never flip EmailConfirmed).
        if (verify.Data.Purpose != OtpPurpose.EmailVerification)
            return Result<VerifyEmailOtpResponseDto>.Failure(ErrorCodes.OtpInvalid, "Invalid verification code.");
        if (!verify.Data.UserId.HasValue)
            return Result<VerifyEmailOtpResponseDto>.Failure(ErrorCodes.OtpInvalid, "Invalid verification code.");

        User? user;
        try
        {
            user = await _userManager.FindByIdAsync(verify.Data.UserId.Value.ToString());
        }
        catch (Exception dbEx)
        {
            _logger.LogError(dbEx, "Account email OTP verify: user lookup failed.");
            return Result<VerifyEmailOtpResponseDto>.Failure(
                ErrorCodes.Exception,
                "The verification service is temporarily unavailable. Please try again in a moment.");
        }

        if (user is null)
            return Result<VerifyEmailOtpResponseDto>.Failure(ErrorCodes.NotFound, "Account not found.");

        // The OTP session is bound to this user id, so we confirm exactly the
        // right account — never a wrong-user confirm.
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation(
                "Email verification persisted (EmailConfirmed=true) for user {UserId}.", user.Id);
        }

        return Result<VerifyEmailOtpResponseDto>.Success(
            new VerifyEmailOtpResponseDto { Verified = true }, "Email verified.");
    }

    /// <inheritdoc />
    public async Task MarkPhoneConfirmedAsync(string rawPhone, string? normalizedPhone)
    {
        try
        {
            // Prefer the Twilio-normalised E.164; fall back to normalising the
            // raw input. Bail if neither yields a phone shape.
            var e164 = string.IsNullOrWhiteSpace(normalizedPhone)
                ? NormalisePhone(rawPhone ?? string.Empty)
                : normalizedPhone!.Trim();
            if (string.IsNullOrWhiteSpace(e164)) return;

            // The account may have stored the phone as E.164 ("+27…"), local
            // ("0…"), or exactly what was typed — match all three. Coalesce so
            // no candidate is null (a null EF comparison becomes "IS NULL" and
            // would wrongly match phone-less accounts).
            var local = E164ToLocal(e164) ?? e164;
            var raw = string.IsNullOrWhiteSpace(rawPhone) ? e164 : rawPhone!.Trim();

            var matches = await _userManager.Users
                .Where(u => u.PhoneNumber != null &&
                            (u.PhoneNumber == e164 || u.PhoneNumber == local || u.PhoneNumber == raw))
                .ToListAsync();

            // Only confirm when EXACTLY one account owns the number — never risk
            // confirming the wrong user if a phone was somehow duplicated.
            if (matches.Count != 1)
            {
                _logger.LogWarning(
                    "MarkPhoneConfirmed: {Count} accounts matched the phone — skipping to avoid a wrong-user confirm.",
                    matches.Count);
                return;
            }

            var user = matches[0];
            if (!user.PhoneNumberConfirmed)
            {
                user.PhoneNumberConfirmed = true;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation(
                    "Phone verification persisted (PhoneNumberConfirmed=true) for user {UserId}.", user.Id);
            }
        }
        catch (Exception ex)
        {
            // Best-effort — must never break the verify-otp response.
            _logger.LogError(ex, "MarkPhoneConfirmed failed.");
        }
    }

    /// <summary>"+27XXXXXXXXX" → "0XXXXXXXXX" (SA local form), else null.</summary>
    private static string? E164ToLocal(string e164)
    {
        if (string.IsNullOrWhiteSpace(e164)) return null;
        if (e164.StartsWith("+27") && e164.Length > 3) return "0" + e164.Substring(3);
        return null;
    }

    /// <inheritdoc />
    public async Task<Result> LogoutAsync(Guid userId)
    {
        try
        {
            await _jwtTokenGenerator.RevokeAllRefreshTokensForUserAsync(userId);
            return Result.Success("Logged out successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed for user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Logout failed.");
        }
    }
}