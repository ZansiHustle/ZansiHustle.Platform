using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Payments.External.Dtos;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Application.Persistence.Payments;
using ZansiHustle.Domain.Payments;
using ZansiHustle.Domain.Payments.External;
using ZansiHustle.Shared.Enums.Payments;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Payments.External
{
    /// <summary>
    /// Application service for the External Shop Payments feature. Reuses
    /// the SAME <see cref="IOzowClient"/>/<see cref="IOzowHashService"/> the
    /// internal marketplace checkout uses — same Ozow merchant credentials,
    /// same hash algorithm — but every Ozow-facing URL (Success/Cancel/Error/
    /// NotifyUrl) is a dedicated external-payments endpoint, so this feature
    /// shares the provider without touching <c>PaymentService</c> or
    /// <c>PaymentsController</c> at all. Webhook idempotency reuses the
    /// existing <see cref="PaymentEvent"/> table (its ProviderEventKey unique
    /// index and PaymentId is nullable by design) via <see cref="IPaymentRepository"/>.
    /// </summary>
    public sealed class ExternalShopPaymentService : IExternalShopPaymentService
    {
        private static readonly JsonSerializerOptions CallbackJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static readonly ExternalPaymentSessionStatus[] TerminalFailedStatuses =
        {
            ExternalPaymentSessionStatus.Failed,
            ExternalPaymentSessionStatus.Cancelled,
            ExternalPaymentSessionStatus.Expired,
        };

        private readonly IExternalPaymentSessionRepository _sessionRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOzowClient _ozowClient;
        private readonly IOzowHashService _ozowHashService;
        private readonly IExternalShopSignatureService _signatureService;
        private readonly IExternalShopCallbackSender _callbackSender;
        private readonly ExternalShopsOptions _shops;
        private readonly ExternalPaymentsSettings _settings;
        private readonly ILogger<ExternalShopPaymentService> _logger;

        public ExternalShopPaymentService(
            IExternalPaymentSessionRepository sessionRepository,
            IPaymentRepository paymentRepository,
            IOzowClient ozowClient,
            IOzowHashService ozowHashService,
            IExternalShopSignatureService signatureService,
            IExternalShopCallbackSender callbackSender,
            IOptions<ExternalShopsOptions> shops,
            IOptions<ExternalPaymentsSettings> settings,
            ILogger<ExternalShopPaymentService> logger)
        {
            _sessionRepository = sessionRepository;
            _paymentRepository = paymentRepository;
            _ozowClient = ozowClient;
            _ozowHashService = ozowHashService;
            _signatureService = signatureService;
            _callbackSender = callbackSender;
            _shops = shops.Value ?? new ExternalShopsOptions();
            _settings = settings.Value ?? new ExternalPaymentsSettings();
            _logger = logger;
        }

        // ─── Create session ────────────────────────────────────────────────

        public async Task<Result<CreateExternalPaymentSessionResponseDto>> CreateSessionAsync(
            CreateExternalPaymentSessionRequestDto? request,
            string? providedSharedSecret,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                return Fail(ErrorCodes.BadRequest, "Request body is required.");

            if (string.IsNullOrWhiteSpace(providedSharedSecret))
                return Fail(ErrorCodes.Unauthorized, "Missing X-ZansiHustle-Shared-Secret header.");

            if (string.IsNullOrWhiteSpace(request.ShopCode))
                return Fail(ErrorCodes.BadRequest, "shopCode is required.");
            if (string.IsNullOrWhiteSpace(request.ExternalOrderId))
                return Fail(ErrorCodes.BadRequest, "externalOrderId is required.");
            if (string.IsNullOrWhiteSpace(request.ExternalOrderNumber))
                return Fail(ErrorCodes.BadRequest, "externalOrderNumber is required.");
            if (request.Amount <= 0)
                return Fail(ErrorCodes.BadRequest, "amount must be greater than zero.");
            if (!string.Equals(request.Currency, "ZAR", StringComparison.OrdinalIgnoreCase))
                return Fail(ErrorCodes.BadRequest, "Only ZAR is supported.");
            if (string.IsNullOrWhiteSpace(request.ReturnUrl))
                return Fail(ErrorCodes.BadRequest, "returnUrl is required.");
            if (string.IsNullOrWhiteSpace(request.CallbackUrl))
                return Fail(ErrorCodes.BadRequest, "callbackUrl is required.");

            var shopCodeKey = request.ShopCode.Trim().ToLowerInvariant();

            if (!_shops.TryGetValue(shopCodeKey, out var shop) || shop is null)
            {
                _logger.LogWarning("[ExternalPayments][Create] unknown shopCode={ShopCode}", request.ShopCode);
                return Fail(ErrorCodes.Unauthorized, "Unknown shop.");
            }

            if (!ConstantTimeEquals(providedSharedSecret, shop.SharedSecret))
            {
                _logger.LogWarning("[ExternalPayments][Create] invalid shared secret for shopCode={ShopCode}", shopCodeKey);
                return Fail(ErrorCodes.Unauthorized, "Invalid shared secret.");
            }

            if (!shop.Enabled)
            {
                _logger.LogWarning("[ExternalPayments][Create] shop disabled shopCode={ShopCode}", shopCodeKey);
                return Fail(ErrorCodes.Forbidden, "This shop is not enabled for payments.");
            }

            if (!TryValidateUrl(request.ReturnUrl, shop.AllowedReturnHosts, out var returnUrlError))
                return Fail(ErrorCodes.BadRequest, $"returnUrl invalid: {returnUrlError}");
            if (!TryValidateUrl(request.CallbackUrl, shop.AllowedCallbackHosts, out var callbackUrlError))
                return Fail(ErrorCodes.BadRequest, $"callbackUrl invalid: {callbackUrlError}");

            var externalOrderId = request.ExternalOrderId.Trim();

            // ── Idempotency: same shop + same external order + still-active session → reuse ──
            var existing = await _sessionRepository.GetActiveByShopAndExternalOrderAsync(shopCodeKey, externalOrderId);
            if (existing is not null)
            {
                if (existing.Amount != request.Amount ||
                    !string.Equals(existing.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "[ExternalPayments][Create] idempotent replay amount/currency mismatch shop={ShopCode} order={OrderId} existing={ExAmt}{ExCcy} requested={ReqAmt}{ReqCcy}. Returning the existing session unchanged.",
                        shopCodeKey, externalOrderId, existing.Amount, existing.Currency, request.Amount, request.Currency);
                }

                if (string.IsNullOrWhiteSpace(existing.ProviderAuthorizationUrl))
                    return Fail(ErrorCodes.PaymentInitFailed, "An existing payment attempt for this order has no redirect URL yet. Please retry shortly.");

                return Result<CreateExternalPaymentSessionResponseDto>.Success(
                    new CreateExternalPaymentSessionResponseDto { SessionId = existing.Id, RedirectUrl = existing.ProviderAuthorizationUrl! },
                    "Existing payment session reused.");
            }

            if (!_ozowClient.IsConfigured)
            {
                var missing = _ozowClient.GetMissingFieldEnvVars();
                var msg = missing.Count > 0
                    ? $"Ozow is not configured on this environment. Missing: {string.Join(", ", missing)}."
                    : "Ozow is not configured on this environment.";
                return Fail(ErrorCodes.ProviderNotConfigured, msg);
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiBaseUrl))
            {
                _logger.LogError("[ExternalPayments][Create] ExternalPayments:ApiBaseUrl is not configured — cannot build Ozow return/notify URLs.");
                return Fail(ErrorCodes.ProviderNotConfigured, "External payments API base URL is not configured on this environment.");
            }

            var uatTestMode = _ozowClient.UatTestMode;
            var chargedAmount = uatTestMode ? Math.Min(request.Amount, _ozowClient.UatTestAmount) : request.Amount;

            var session = new ExternalPaymentSession
            {
                Id = Guid.NewGuid(),
                ShopCode = shopCodeKey,
                ShopName = shop.ShopName,
                ExternalOrderId = externalOrderId,
                ExternalOrderNumber = request.ExternalOrderNumber.Trim(),
                Amount = chargedAmount,
                Currency = "ZAR",
                CustomerEmail = request.CustomerEmail,
                ReturnUrl = request.ReturnUrl.Trim(),
                CallbackUrl = request.CallbackUrl.Trim(),
                Provider = PaymentProvider.Ozow,
                Status = ExternalPaymentSessionStatus.Pending,
                IsTest = uatTestMode,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _sessionRepository.AddAsync(session);
            try
            {
                await _sessionRepository.SaveChangesAsync();
            }
            catch (DbUpdateException dbex) when (IsUniqueViolation(dbex))
            {
                // A concurrent retry raced us into the partial-unique index — fetch what the other request created.
                var raced = await _sessionRepository.GetActiveByShopAndExternalOrderAsync(shopCodeKey, externalOrderId);
                if (raced is not null && !string.IsNullOrWhiteSpace(raced.ProviderAuthorizationUrl))
                    return Result<CreateExternalPaymentSessionResponseDto>.Success(
                        new CreateExternalPaymentSessionResponseDto { SessionId = raced.Id, RedirectUrl = raced.ProviderAuthorizationUrl! },
                        "Existing payment session reused.");
                return Fail(ErrorCodes.Conflict, "A concurrent request already created a payment session for this order.");
            }

            if (uatTestMode)
            {
                _logger.LogWarning(
                    "[ExternalPayments][Ozow][UAT-TEST] session {SessionId} shop={ShopCode} order={OrderId} charge capped at R{Charged} (requested R{Requested}).",
                    session.Id, shopCodeKey, externalOrderId, session.Amount, request.Amount);
            }

            var code = "EXTPAY_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var bankReference = BuildBankReference(code);

            var ozowRequest = new OzowPaymentRequest
            {
                Amount = session.Amount,
                TransactionReference = code,
                BankReference = bankReference,
                Optional1 = session.Id.ToString(),
                Optional2 = session.ShopCode,
                Optional3 = session.ExternalOrderId,
                SuccessUrl = CombineUrl(_settings.ApiBaseUrl, "external-payments/ozow/return/success"),
                CancelUrl = CombineUrl(_settings.ApiBaseUrl, "external-payments/ozow/return/cancel"),
                ErrorUrl = CombineUrl(_settings.ApiBaseUrl, "external-payments/ozow/return/error"),
                NotifyUrl = CombineUrl(_settings.ApiBaseUrl, "external-payments/ozow/webhook"),
            };

            var ozowResult = await _ozowClient.CreatePaymentRequestAsync(ozowRequest, cancellationToken);

            if (!ozowResult.IsSuccess || ozowResult.Data is null || string.IsNullOrWhiteSpace(ozowResult.Data.Url))
            {
                session.Status = ExternalPaymentSessionStatus.Failed;
                session.FailureReason = ozowResult.Message;
                session.ProviderReference = code;
                session.UpdatedAtUtc = DateTime.UtcNow;
                _sessionRepository.Update(session);
                await _sessionRepository.SaveChangesAsync();

                var forwardedCode = string.IsNullOrWhiteSpace(ozowResult.Code) ? ErrorCodes.PaymentProviderUnavailable : ozowResult.Code;
                return Fail(forwardedCode, ozowResult.Message ?? "Failed to initialize Ozow payment.");
            }

            session.Status = ExternalPaymentSessionStatus.RedirectCreated;
            session.ProviderReference = code;
            session.ProviderAuthorizationUrl = ozowResult.Data.Url;
            session.ProviderAccessCode = ozowResult.Data.PaymentRequestId;
            session.UpdatedAtUtc = DateTime.UtcNow;
            _sessionRepository.Update(session);
            await _sessionRepository.SaveChangesAsync();

            _logger.LogInformation(
                "[ExternalPayments][Create] session {SessionId} created shop={ShopCode} order={OrderId} amount={Amount}.",
                session.Id, session.ShopCode, session.ExternalOrderId, session.Amount);

            return Result<CreateExternalPaymentSessionResponseDto>.Success(
                new CreateExternalPaymentSessionResponseDto { SessionId = session.Id, RedirectUrl = ozowResult.Data.Url! },
                "Payment session created.");
        }

        // ─── Status ────────────────────────────────────────────────────────

        public async Task<Result<ExternalPaymentSessionStatusResponseDto>> GetStatusAsync(
            Guid sessionId,
            string? providedSharedSecret,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(providedSharedSecret))
                return Result<ExternalPaymentSessionStatusResponseDto>.Failure(ErrorCodes.Unauthorized, "Missing X-ZansiHustle-Shared-Secret header.");

            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session is null)
                return Result<ExternalPaymentSessionStatusResponseDto>.Failure(ErrorCodes.NotFound, "Payment session not found.");

            if (!_shops.TryGetValue(session.ShopCode, out var shop) || shop is null ||
                !ConstantTimeEquals(providedSharedSecret, shop.SharedSecret))
            {
                // Same response as "not found" — never confirm a session's existence
                // to a caller who isn't its owning shop.
                _logger.LogWarning("[ExternalPayments][Status] ownership check failed for session {SessionId}.", sessionId);
                return Result<ExternalPaymentSessionStatusResponseDto>.Failure(ErrorCodes.NotFound, "Payment session not found.");
            }

            await MaybeExpireAsync(session);

            if (IsTerminal(session.Status) && !session.CallbackDeliveredAtUtc.HasValue)
            {
                // Opportunistic retry: a status poll is also a chance to redeliver
                // a callback that failed the first time — no background job needed.
                await TryDeliverCallbackAsync(session, cancellationToken);
            }

            return Result<ExternalPaymentSessionStatusResponseDto>.Success(
                new ExternalPaymentSessionStatusResponseDto
                {
                    Status = session.Status.ToString(),
                    FailureReason = session.FailureReason,
                },
                "OK");
        }

        // ─── Browser return ────────────────────────────────────────────────

        public async Task<Uri?> ResolveBrowserReturnAsync(string? transactionReference, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(transactionReference))
                return null;

            var session = await _sessionRepository.GetByProviderReferenceAsync(transactionReference);
            if (session is null)
                return null;

            // Best-effort reconciliation via Ozow's OWN transaction lookup — never
            // from the unverified query string the browser carried back.
            if (!IsTerminal(session.Status) && _ozowClient.IsConfigured)
            {
                try
                {
                    var lookup = await _ozowClient.GetTransactionByReferenceAsync(transactionReference, cancellationToken);
                    if (lookup.IsSuccess && lookup.Data is not null)
                    {
                        await ApplyOzowSignalAsync(session, lookup.Data.Status ?? "Unknown", lookup.Data.Amount, lookup.Data.CurrencyCode, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[ExternalPayments][Return] best-effort Ozow lookup failed for ref {Ref}.", transactionReference);
                }
            }

            var separator = session.ReturnUrl.Contains('?') ? '&' : '?';
            return new Uri($"{session.ReturnUrl}{separator}session={Uri.EscapeDataString(session.Id.ToString())}");
        }

        // ─── Webhook (authoritative) ───────────────────────────────────────

        public async Task HandleOzowWebhookAsync(OzowTransactionNotification notification, string rawBody, CancellationToken cancellationToken = default)
        {
            var hashValid = _ozowHashService.ValidateNotificationHash(notification);
            var reference = notification.TransactionReference ?? string.Empty;
            var status = notification.Status ?? "Unknown";
            var transactionId = notification.TransactionId ?? string.Empty;

            // Namespaced distinctly from the internal PaymentService event-key
            // format so the two can never collide even in the unlikely event
            // a reference string were ever reused.
            var eventKey = $"ozow:external:{status}:{reference}:{transactionId}";

            if (await _paymentRepository.EventKeyExistsAsync(eventKey))
                return; // duplicate webhook — already processed.

            var session = await _sessionRepository.GetByProviderReferenceAsync(reference);

            var eventRow = new PaymentEvent
            {
                Id = Guid.NewGuid(),
                PaymentId = null,
                Provider = PaymentProvider.Ozow,
                ProviderEventKey = eventKey,
                EventType = status,
                RawPayload = rawBody ?? string.Empty,
                SignatureHeader = notification.Hash,
                SignatureValid = hashValid,
                Processed = false,
                ReceivedAtUtc = DateTime.UtcNow,
            };

            await _paymentRepository.AddEventAsync(eventRow);
            try
            {
                await _paymentRepository.SaveChangesAsync();
            }
            catch (DbUpdateException dbex) when (IsUniqueViolation(dbex))
            {
                return; // a concurrent duplicate already recorded this event.
            }

            if (!hashValid)
            {
                eventRow.ProcessingError = "Hash mismatch or hash service unavailable.";
                await _paymentRepository.SaveChangesAsync();
                return;
            }

            if (session is null)
            {
                eventRow.ProcessingError = $"No external payment session for reference {reference}.";
                await _paymentRepository.SaveChangesAsync();
                return;
            }

            await ApplyOzowSignalAsync(session, status, notification.Amount, notification.CurrencyCode, cancellationToken);

            eventRow.Processed = true;
            eventRow.ProcessedAtUtc = DateTime.UtcNow;
            await _paymentRepository.SaveChangesAsync();
        }

        // ─── Shared signal-application logic (webhook + browser-return reconciliation) ──

        private async Task ApplyOzowSignalAsync(
            ExternalPaymentSession session,
            string status,
            decimal providerAmount,
            string? providerCurrency,
            CancellationToken cancellationToken)
        {
            // Paid is a one-way door — no later signal (duplicate, out-of-order,
            // or genuinely different) may ever move it anywhere else.
            if (session.Status == ExternalPaymentSessionStatus.Paid)
                return;

            var statusKey = status.Trim().ToLowerInvariant();
            var wasTerminalBefore = IsTerminal(session.Status);

            switch (statusKey)
            {
                case "complete":
                    var currencyOk = string.Equals(providerCurrency ?? session.Currency, session.Currency, StringComparison.OrdinalIgnoreCase);
                    var amountOk = Math.Abs(providerAmount - session.Amount) <= 0.01m;
                    if (!currencyOk || !amountOk)
                    {
                        session.Status = ExternalPaymentSessionStatus.Failed;
                        session.FailureReason = $"Provider amount/currency mismatch (expected {session.Amount} {session.Currency}, got {providerAmount} {providerCurrency}).";
                        _logger.LogError(
                            "[ExternalPayments][Signal] AMOUNT/CURRENCY MISMATCH session={SessionId} expected={ExpAmt}{ExpCcy} got={GotAmt}{GotCcy} — marking Failed, NOT Paid.",
                            session.Id, session.Amount, session.Currency, providerAmount, providerCurrency);
                    }
                    else
                    {
                        session.Status = ExternalPaymentSessionStatus.Paid;
                        session.PaidAtUtc = DateTime.UtcNow;
                        session.FailureReason = null;
                    }
                    break;

                case "cancelled":
                    session.Status = ExternalPaymentSessionStatus.Cancelled;
                    session.FailureReason ??= "Cancelled.";
                    break;

                case "error":
                case "abandoned":
                    session.Status = ExternalPaymentSessionStatus.Failed;
                    session.FailureReason ??= "Payment failed.";
                    break;

                case "pending":
                case "pendinginvestigation":
                    if (!IsTerminal(session.Status))
                        session.Status = ExternalPaymentSessionStatus.Processing;
                    break;

                default:
                    _logger.LogWarning("[ExternalPayments][Signal] unhandled Ozow status '{Status}' for session {SessionId}.", status, session.Id);
                    return;
            }

            session.UpdatedAtUtc = DateTime.UtcNow;
            _sessionRepository.Update(session);
            await _sessionRepository.SaveChangesAsync();

            if (!wasTerminalBefore && IsTerminal(session.Status))
            {
                await TryDeliverCallbackAsync(session, cancellationToken);
            }
        }

        // ─── Expiry (opportunistic — no background job) ───────────────────

        private async Task MaybeExpireAsync(ExternalPaymentSession session)
        {
            if (IsTerminal(session.Status))
                return;

            var ageHours = (DateTime.UtcNow - session.CreatedAtUtc).TotalHours;
            if (ageHours < Math.Max(1, _settings.SessionExpiryHours))
                return;

            session.Status = ExternalPaymentSessionStatus.Expired;
            session.FailureReason ??= "Session expired with no resolution from the provider.";
            session.UpdatedAtUtc = DateTime.UtcNow;
            _sessionRepository.Update(session);
            await _sessionRepository.SaveChangesAsync();

            _logger.LogInformation("[ExternalPayments] session {SessionId} opportunistically expired after {Hours:F1}h.", session.Id, ageHours);
        }

        // ─── Callback delivery ─────────────────────────────────────────────

        private async Task TryDeliverCallbackAsync(ExternalPaymentSession session, CancellationToken cancellationToken)
        {
            if (session.CallbackDeliveredAtUtc.HasValue)
                return;
            if (!IsTerminal(session.Status))
                return;

            if (!_shops.TryGetValue(session.ShopCode, out var shop) || shop is null)
            {
                session.LastCallbackError = "Shop configuration no longer exists.";
                session.UpdatedAtUtc = DateTime.UtcNow;
                _sessionRepository.Update(session);
                await _sessionRepository.SaveChangesAsync();
                return;
            }

            var callback = new ExternalPaymentCallbackDto
            {
                ShopCode = session.ShopCode,
                ProviderSessionId = session.Id.ToString(),
                ExternalOrderId = session.ExternalOrderId,
                ExternalOrderNumber = session.ExternalOrderNumber,
                Amount = session.Amount,
                Currency = session.Currency,
                Status = session.Status.ToString(),
                FailureReason = session.FailureReason,
                OccurredAtUtc = session.PaidAtUtc ?? session.UpdatedAtUtc ?? DateTime.UtcNow,
            };

            // Serialize EXACTLY ONCE — the same bytes are signed and sent.
            var json = JsonSerializer.Serialize(callback, CallbackJsonOptions);
            var bodyBytes = Encoding.UTF8.GetBytes(json);
            var signatureHex = _signatureService.ComputeSignatureHex(shop.SharedSecret, bodyBytes);

            session.CallbackAttemptCount += 1;

            var (ok, error) = await _callbackSender.SendAsync(session.CallbackUrl, bodyBytes, signatureHex, cancellationToken);

            if (ok)
            {
                session.CallbackDeliveredAtUtc = DateTime.UtcNow;
                session.LastCallbackError = null;
                _logger.LogInformation("[ExternalPayments][Callback] delivered session={SessionId} shop={ShopCode} status={Status} attempt={Attempt}.",
                    session.Id, session.ShopCode, session.Status, session.CallbackAttemptCount);
            }
            else
            {
                session.LastCallbackError = error;
                // The Paid/Failed/Cancelled/Expired status itself is NEVER
                // touched here — a merchant-side outage never undoes a
                // legitimately settled payment. Status GET stays authoritative.
                _logger.LogWarning("[ExternalPayments][Callback] delivery FAILED session={SessionId} shop={ShopCode} attempt={Attempt} error={Error}.",
                    session.Id, session.ShopCode, session.CallbackAttemptCount, error);
            }

            session.UpdatedAtUtc = DateTime.UtcNow;
            _sessionRepository.Update(session);
            await _sessionRepository.SaveChangesAsync();
        }

        // ─── Helpers ───────────────────────────────────────────────────────

        private static bool IsTerminal(ExternalPaymentSessionStatus status) =>
            status is ExternalPaymentSessionStatus.Paid
                or ExternalPaymentSessionStatus.Failed
                or ExternalPaymentSessionStatus.Cancelled
                or ExternalPaymentSessionStatus.Expired;

        private static Result<CreateExternalPaymentSessionResponseDto> Fail(string code, string message) =>
            Result<CreateExternalPaymentSessionResponseDto>.Failure(code, message);

        private static bool ConstantTimeEquals(string a, string b)
        {
            var aBytes = Encoding.UTF8.GetBytes(a ?? string.Empty);
            var bBytes = Encoding.UTF8.GetBytes(b ?? string.Empty);
            if (aBytes.Length != bBytes.Length) return false;
            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

        private static bool TryValidateUrl(string url, System.Collections.Generic.IReadOnlyCollection<string> allowedHosts, out string error)
        {
            error = string.Empty;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                error = "must be an absolute URL.";
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                error = "must use HTTPS.";
                return false;
            }

            var host = uri.Host;

            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1" || host == "::1")
            {
                error = "localhost is not allowed.";
                return false;
            }

            if (IPAddress.TryParse(host, out var ip) && IsPrivateOrLoopback(ip))
            {
                error = "private/loopback IP addresses are not allowed.";
                return false;
            }

            if (allowedHosts is null || allowedHosts.Count == 0 || !allowedHosts.Any(h => string.Equals(h, host, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"host '{host}' is not in the allowed list for this shop.";
                return false;
            }

            return true;
        }

        private static bool IsPrivateOrLoopback(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip)) return true;
            if (ip.AddressFamily != AddressFamily.InterNetwork) return false;

            var b = ip.GetAddressBytes();
            return b[0] == 10
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 169 && b[1] == 254); // link-local
        }

        private static string CombineUrl(string baseUrl, string path) =>
            baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');

        private static string BuildBankReference(string code)
        {
            var sanitized = new string(code.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            return sanitized.Length <= 20 ? sanitized : sanitized[..20];
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException?.Message.Contains("2601") == true
                || ex.InnerException?.Message.Contains("2627") == true
                || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
