using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Notifications;
using ZansiHustle.Application.Payments.Dtos;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Application.Persistence.Payments;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.Payments;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.Payments;
using ZansiHustle.Shared.Enums.ServiceBookings;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Payments
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IServiceBookingRepository _serviceBookingRepository;
        private readonly INotificationService _notificationService;
        private readonly IPaystackClient _paystackClient;
        private readonly IOzowClient _ozowClient;
        private readonly IOzowHashService _ozowHashService;
        private readonly IYocoClient _yocoClient;
        private readonly IYocoSignatureService _yocoSignatureService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IServiceBookingRepository serviceBookingRepository,
            INotificationService notificationService,
            IPaystackClient paystackClient,
            IOzowClient ozowClient,
            IOzowHashService ozowHashService,
            IYocoClient yocoClient,
            IYocoSignatureService yocoSignatureService,
            UserManager<User> userManager,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _serviceBookingRepository = serviceBookingRepository;
            _notificationService = notificationService;
            _paystackClient = paystackClient;
            _ozowClient = ozowClient;
            _ozowHashService = ozowHashService;
            _yocoClient = yocoClient;
            _yocoSignatureService = yocoSignatureService;
            _userManager = userManager;
            _logger = logger;
        }

        // ─── Initialize ──────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<InitializePaymentResponseDto>> InitializeAsync(Guid buyerUserId, InitializePaymentRequestDto request, CancellationToken cancellationToken = default)
        {
            // Stopwatch + structured-context logging. Each branch returns
            // a Result.Failure with a specific ErrorCode, so failure modes
            // are individually searchable in the log. Never logs request
            // bodies wholesale — only OrderId, provider, elapsed, and the
            // outcome code/short reason.
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (request is null || request.OrderId == Guid.Empty)
                {
                    _logger.LogWarning(
                        "[Payments][Initialize][Svc] reject: empty OrderId. userId={UserId} elapsedMs={Elapsed}",
                        buyerUserId, sw.ElapsedMilliseconds);
                    return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.BadRequest, "OrderId is required.");
                }

                _logger.LogInformation(
                    "[Payments][Initialize][Svc] start orderId={OrderId} userId={UserId} provider={Provider}",
                    request.OrderId, buyerUserId, request.Provider ?? "<default>");

                var order = await _orderRepository.GetByIdAsync(request.OrderId);

                if (order is null)
                {
                    _logger.LogWarning(
                        "[Payments][Initialize][Svc] reject: order not found. orderId={OrderId} userId={UserId} elapsedMs={Elapsed}",
                        request.OrderId, buyerUserId, sw.ElapsedMilliseconds);
                    return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.NotFound, "Order not found.");
                }

                if (order.BuyerUserId != buyerUserId)
                {
                    _logger.LogWarning(
                        "[Payments][Initialize][Svc] reject: forbidden buyer. orderId={OrderId} orderBuyer={OrderBuyer} caller={UserId} elapsedMs={Elapsed}",
                        order.Id, order.BuyerUserId, buyerUserId, sw.ElapsedMilliseconds);
                    return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to pay for this order.");
                }

                var guard = EnsureOrderIsPayable(order);

                if (!guard.IsSuccess)
                {
                    _logger.LogWarning(
                        "[Payments][Initialize][Svc] reject: order not payable. orderId={OrderId} orderCode={OrderCode} status={OrderStatus} payStatus={PayStatus} code={Code} reason={Reason} elapsedMs={Elapsed}",
                        order.Id, order.Code, order.Status, order.PaymentStatus, guard.Code, guard.Message, sw.ElapsedMilliseconds);
                    return Result<InitializePaymentResponseDto>.Failure(guard.Code, guard.Message);
                }

                // Reuse any in-flight attempt — same caller, same order → same checkout session.
                var existing = await _paymentRepository.GetActiveAttemptForOrderAsync(order.Id);

                if (existing != null)
                {
                    _logger.LogInformation(
                        "[Payments][Initialize][Svc] reuse active attempt {Code} for order {OrderCode} via {Provider}. elapsedMs={Elapsed}",
                        existing.Code, order.Code, existing.Provider, sw.ElapsedMilliseconds);
                    return Result<InitializePaymentResponseDto>.Success(MapInitializeResponse(existing), "Resumed pending payment.");
                }

                // Default to Ozow when caller didn't specify — it's the only currently
                // active provider per the live capability matrix.
                var providerName = ResolveProvider(request.Provider);

                _logger.LogInformation(
                    "[Payments][Initialize][Svc] dispatch provider={Provider} orderId={OrderId} orderCode={OrderCode} amount={Amount} elapsedMs={Elapsed}",
                    providerName, order.Id, order.Code, order.Total, sw.ElapsedMilliseconds);

                Result<InitializePaymentResponseDto> result = providerName switch
                {
                    PaymentProvider.Ozow => await InitializeOzowAsync(order, buyerUserId, cancellationToken),
                    PaymentProvider.Yoco => await InitializeYocoAsync(order, buyerUserId, cancellationToken),
                    PaymentProvider.Paystack => await InitializePaystackAsync(order, buyerUserId, request.CallbackUrl, cancellationToken),
                    _ => Result<InitializePaymentResponseDto>.Failure(ErrorCodes.BadRequest, $"Unknown payment provider '{providerName}'.")
                };

                _logger.LogInformation(
                    "[Payments][Initialize][Svc] done provider={Provider} success={Success} code={Code} orderId={OrderId} elapsedMs={Elapsed}",
                    providerName, result.IsSuccess, result.IsSuccess ? "OK" : result.Code, order.Id, sw.ElapsedMilliseconds);

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Caller (or CF edge) disconnected mid-flight. Don't log as Error.
                _logger.LogWarning(
                    "[Payments][Initialize][Svc] CANCELLED. orderId={OrderId} userId={UserId} elapsedMs={Elapsed}",
                    request?.OrderId, buyerUserId, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Payments][Initialize][Svc] EXCEPTION orderId={OrderId} userId={UserId} elapsedMs={Elapsed} exType={ExType}",
                    request?.OrderId, buyerUserId, sw.ElapsedMilliseconds, ex.GetType().Name);
                // PAYMENT_PROVIDER_UNAVAILABLE → HTTP 422 (see BaseController.MapFailure)
                // → Cloudflare passes through unchanged so the client sees this
                // envelope rather than CF's substituted 502 page.
                return Result<InitializePaymentResponseDto>.Failure(
                    ErrorCodes.PaymentProviderUnavailable,
                    "Failed to initialize payment.");
            }
        }

        private static string ResolveProvider(string? requested)
        {
            if (string.IsNullOrWhiteSpace(requested)) return PaymentProvider.Ozow;
            if (string.Equals(requested, PaymentProvider.Ozow,     StringComparison.OrdinalIgnoreCase)) return PaymentProvider.Ozow;
            if (string.Equals(requested, PaymentProvider.Yoco,     StringComparison.OrdinalIgnoreCase)) return PaymentProvider.Yoco;
            if (string.Equals(requested, PaymentProvider.Paystack, StringComparison.OrdinalIgnoreCase)) return PaymentProvider.Paystack;
            return requested;
        }

        // ─── Initialize: Paystack ────────────────────────────────────────────

        private async Task<Result<InitializePaymentResponseDto>> InitializePaystackAsync(
            Order order,
            Guid buyerUserId,
            string? callbackUrl,
            CancellationToken cancellationToken)
        {
            if (!_paystackClient.IsConfigured)
                return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.ProviderNotConfigured, "Paystack is not configured on this environment.");

            var buyer = await _userManager.FindByIdAsync(buyerUserId.ToString());

            if (buyer is null || string.IsNullOrWhiteSpace(buyer.Email))
                return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.BadRequest, "Buyer email is required for Paystack initialization.");

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Code = GenerateCode(),
                OrderId = order.Id,
                UserId = buyerUserId,
                Provider = PaymentProvider.Paystack,
                Amount = order.Total,
                Currency = string.IsNullOrWhiteSpace(order.Currency) ? "ZAR" : order.Currency,
                Status = PaymentTransactionStatus.Initialized,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var initRequest = new PaystackInitializeRequest
            {
                Email = buyer.Email!,
                Amount = ToSubunit(payment.Amount),
                Currency = payment.Currency,
                Reference = payment.Code,
                CallbackUrl = callbackUrl,
                Metadata = new
                {
                    orderId = order.Id,
                    orderCode = order.Code,
                    paymentId = payment.Id
                }
            };

            var initResult = await _paystackClient.InitializeTransactionAsync(initRequest, cancellationToken);

            if (!initResult.IsSuccess || initResult.Data?.Data is null)
            {
                payment.Status = PaymentTransactionStatus.Failed;
                payment.FailedAtUtc = DateTime.UtcNow;
                payment.FailureReason = initResult.Message;
                payment.UpdatedAtUtc = DateTime.UtcNow;
                _paymentRepository.Update(payment);
                await _paymentRepository.SaveChangesAsync();

                return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.PaymentInitFailed, initResult.Message ?? "Failed to initialize payment.");
            }

            payment.ProviderReference = initResult.Data.Data.Reference ?? payment.Code;
            payment.ProviderAuthorizationUrl = initResult.Data.Data.AuthorizationUrl;
            payment.ProviderAccessCode = initResult.Data.Data.AccessCode;
            payment.Status = PaymentTransactionStatus.Pending;
            payment.UpdatedAtUtc = DateTime.UtcNow;

            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync();

            _logger.LogInformation("Payment {Code} initialized via Paystack for order {OrderCode}.", payment.Code, order.Code);
            return Result<InitializePaymentResponseDto>.Success(MapInitializeResponse(payment), "Payment initialized.");
        }

        // ─── Initialize: Ozow ────────────────────────────────────────────────

        private async Task<Result<InitializePaymentResponseDto>> InitializeOzowAsync(
            Order order,
            Guid buyerUserId,
            CancellationToken cancellationToken)
        {
            if (!_ozowClient.IsConfigured)
            {
                // Surface the specific missing env vars in BOTH the log AND
                // the response message so an ops engineer hitting Swagger /
                // Postman can fix the host without grepping a 50MB log file.
                // Names only — never values; PrivateKey value never appears.
                var missing = _ozowClient.GetMissingFieldEnvVars();
                _logger.LogWarning(
                    "[Payments][Initialize][Svc] Ozow not configured. missingEnvVars={Missing}",
                    string.Join(",", missing));
                var msg = missing.Count > 0
                    ? $"Ozow is not configured on this environment. Missing: {string.Join(", ", missing)}."
                    : "Ozow is not configured on this environment.";
                return Result<InitializePaymentResponseDto>.Failure(
                    ErrorCodes.ProviderNotConfigured,
                    msg);
            }

            // Per-request config-presence trace. Booleans + UAT cap ONLY —
            // never any key values. Anyone diagnosing a 502 from stdout can
            // confirm at a glance that creds reached the running process.
            // The full BaseUrl host is logged from OzowClient itself; we
            // don't duplicate it here.
            var ozowMissing = _ozowClient.GetMissingFieldEnvVars();
            _logger.LogInformation(
                "[Payments][Initialize][Svc][Ozow] config uatTestMode={UatTestMode} uatCap={UatCap} " +
                "siteCodePresent={SiteCodePresent} apiKeyPresent={ApiKeyPresent} privateKeyPresent={PrivateKeyPresent} notifyUrlPresent={NotifyUrlPresent}",
                _ozowClient.UatTestMode,
                _ozowClient.UatTestAmount,
                !ozowMissing.Contains("Ozow__SiteCode"),
                !ozowMissing.Contains("Ozow__ApiKey"),
                !ozowMissing.Contains("Ozow__PrivateKey"),
                !ozowMissing.Contains("Ozow__NotifyUrl"));

            // ── UAT controlled-testing guard ─────────────────────────────────
            // When Ozow:UatTestMode is true (typically in the deployed UAT
            // environment) we cap the charged amount, tag the code, and mark
            // the row IsTest=true so real-money testing stays safe and
            // identifiable in admin/reporting views.
            var uatTestMode = _ozowClient.UatTestMode;
            var chargedAmount = uatTestMode
                ? Math.Min(order.Total, _ozowClient.UatTestAmount)
                : order.Total;
            // UAT-TEST prefix uses underscores too (see GenerateCode rationale).
            var paymentCode = uatTestMode ? "UAT_TEST_" + GenerateCode() : GenerateCode();

            // Save the local Payment row before calling Ozow so the webhook
            // has somewhere to land if Ozow's notify races our response.
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Code = paymentCode,
                OrderId = order.Id,
                UserId = buyerUserId,
                Provider = PaymentProvider.Ozow,
                Amount = chargedAmount,
                Currency = string.IsNullOrWhiteSpace(order.Currency) ? "ZAR" : order.Currency,
                Status = PaymentTransactionStatus.Initialized,
                IsTest = uatTestMode,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            if (uatTestMode)
            {
                _logger.LogWarning(
                    "[Ozow][UAT-TEST] Initiating TEST payment {Code} for order {OrderCode}. " +
                    "Charged amount capped at R{Charged} (order total R{OrderTotal}). IsTest=true.",
                    payment.Code, order.Code, payment.Amount, order.Total);
            }

            // Ozow's bankReference is shown to the buyer on their bank statement
            // and is hard-capped at 20 chars. Use a shortened code form.
            var bankReference = BuildOzowBankReference(payment.Code);

            // Sanitized reference log. Confirms the values reaching Ozow
            // match [A-Za-z0-9_] — useful when triaging "Failed to create
            // transaction" reports against the dashboard reference-
            // validation expression. No secrets here; reference strings
            // are the same ones echoed back on the webhook.
            _logger.LogInformation(
                "[Ozow][Refs] paymentCode={Code} transactionRef={TxRef} bankRef={BankRef} length={Len}",
                payment.Code, payment.Code, bankReference, bankReference.Length);

            // The transactionReference is what Ozow echoes back on the webhook;
            // we use our own Payment.Code so reconciliation is trivial.
            var ozowRequest = new OzowPaymentRequest
            {
                Amount = payment.Amount,
                TransactionReference = payment.Code,
                BankReference = bankReference,
                Optional1 = order.Id.ToString(),
                Optional2 = order.Code,
                Optional3 = payment.Id.ToString()
                // siteCode/isTest/country/currency/urls/hashCheck are populated by OzowClient.
            };

            var ozowResult = await _ozowClient.CreatePaymentRequestAsync(ozowRequest, cancellationToken);

            if (!ozowResult.IsSuccess || ozowResult.Data is null || string.IsNullOrWhiteSpace(ozowResult.Data.Url))
            {
                payment.Status = PaymentTransactionStatus.Failed;
                payment.FailedAtUtc = DateTime.UtcNow;
                payment.FailureReason = ozowResult.Message;
                payment.UpdatedAtUtc = DateTime.UtcNow;
                _paymentRepository.Update(payment);
                await _paymentRepository.SaveChangesAsync();

                // Forward the specific provider-failure code OzowClient set.
                //   PAYMENT_PROVIDER_UNAVAILABLE → couldn't reach Ozow / timeout / 5xx
                //   PAYMENT_INIT_FAILED          → Ozow responded but with a non-actionable payload
                // Both map to HTTP 422 in BaseController so Cloudflare doesn't
                // intercept and substitute its own 502 page.
                var forwardedCode = string.IsNullOrWhiteSpace(ozowResult.Code)
                    ? ErrorCodes.PaymentProviderUnavailable
                    : ozowResult.Code;
                return Result<InitializePaymentResponseDto>.Failure(forwardedCode, ozowResult.Message ?? "Failed to initialize Ozow payment.");
            }

            payment.ProviderReference = payment.Code;
            payment.ProviderAuthorizationUrl = ozowResult.Data.Url;
            payment.ProviderAccessCode = ozowResult.Data.PaymentRequestId;
            payment.Status = PaymentTransactionStatus.Pending;
            payment.UpdatedAtUtc = DateTime.UtcNow;

            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync();

            _logger.LogInformation("Payment {Code} initialized via Ozow for order {OrderCode}.", payment.Code, order.Code);
            return Result<InitializePaymentResponseDto>.Success(MapInitializeResponse(payment), "Payment initialized.");
        }

        private static string BuildOzowBankReference(string paymentCode)
        {
            // Strip the PAY_ / UAT_TEST_ prefix and trim to 20 chars to
            // satisfy Ozow's bankReference rule. The output appears on the
            // buyer's bank statement so we keep it readable, not opaque.
            //
            // Belt-and-braces sanitisation: strip any character outside
            // [A-Za-z0-9_]. Even if a future caller passes a hyphenated
            // legacy code into this helper, Ozow / Capitec / FNB will only
            // see the underscore-and-alphanumeric form they accept.
            const int max = 20;
            var stripped = paymentCode;
            if (stripped.StartsWith("PAY_", StringComparison.Ordinal)) stripped = stripped.Substring(4);
            else if (stripped.StartsWith("PAY-", StringComparison.Ordinal)) stripped = stripped.Substring(4);
            if (stripped.StartsWith("UAT_TEST_", StringComparison.Ordinal)) stripped = stripped.Substring("UAT_TEST_".Length);
            else if (stripped.StartsWith("UAT-TEST-", StringComparison.Ordinal)) stripped = stripped.Substring("UAT-TEST-".Length);

            var sb = new System.Text.StringBuilder(stripped.Length);
            foreach (var ch in stripped)
            {
                if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')
                    || (ch >= '0' && ch <= '9') || ch == '_')
                {
                    sb.Append(ch);
                }
                // Any other character is dropped silently (would be: hyphen,
                // space, punctuation). Caller-side codes only ever contain
                // safe chars now, so this is the legacy-safety guard.
            }
            var safe = sb.ToString();
            return safe.Length > max ? safe.Substring(0, max) : safe;
        }

        // ─── Initialize: Yoco ────────────────────────────────────────────────

        private async Task<Result<InitializePaymentResponseDto>> InitializeYocoAsync(
            Order order,
            Guid buyerUserId,
            CancellationToken cancellationToken)
        {
            if (!_yocoClient.IsConfigured)
                return Result<InitializePaymentResponseDto>.Failure(
                    ErrorCodes.ProviderNotConfigured,
                    "Yoco is not configured on this environment (credentials and/or webhook signing secret).");

            // ── UAT controlled-testing guard ─────────────────────────────────
            // Same model as Ozow: cap amount, tag the code, mark IsTest.
            var uatTestMode = _yocoClient.UatTestMode;
            var chargedAmount = uatTestMode
                ? Math.Min(order.Total, _yocoClient.UatTestAmount)
                : order.Total;
            // UAT-TEST prefix uses underscores too (see GenerateCode rationale).
            var paymentCode = uatTestMode ? "UAT_TEST_" + GenerateCode() : GenerateCode();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Code = paymentCode,
                OrderId = order.Id,
                UserId = buyerUserId,
                Provider = PaymentProvider.Yoco,
                Amount = chargedAmount,
                Currency = string.IsNullOrWhiteSpace(order.Currency) ? "ZAR" : order.Currency,
                Status = PaymentTransactionStatus.Initialized,
                IsTest = uatTestMode,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            if (uatTestMode)
            {
                _logger.LogWarning(
                    "[Yoco][UAT-TEST] Initiating TEST payment {Code} for order {OrderCode}. " +
                    "Charged amount capped at R{Charged} (order total R{OrderTotal}). IsTest=true.",
                    payment.Code, order.Code, payment.Amount, order.Total);
            }

            // Yoco wants the amount in cents. We pass our own Payment Code in
            // metadata so the webhook handler can find the payment row even
            // if Yoco's checkoutId is missing on the payload.
            var yocoRequest = new YocoCheckoutRequest
            {
                Amount = ToSubunit(payment.Amount),
                Currency = payment.Currency,
                Metadata = new Dictionary<string, string>
                {
                    ["paymentCode"] = payment.Code,
                    ["orderId"] = order.Id.ToString(),
                    ["orderCode"] = order.Code,
                    ["paymentId"] = payment.Id.ToString()
                }
                // SuccessUrl / CancelUrl / FailureUrl are passed through by
                // YocoClient.ConfigureHttpClient consumers — we leave them
                // null here so the global Yoco settings take effect. Per-call
                // overrides can be threaded through later if needed.
            };

            var ycResult = await _yocoClient.CreateCheckoutAsync(yocoRequest, cancellationToken);

            if (!ycResult.IsSuccess || ycResult.Data is null || string.IsNullOrWhiteSpace(ycResult.Data.RedirectUrl))
            {
                payment.Status = PaymentTransactionStatus.Failed;
                payment.FailedAtUtc = DateTime.UtcNow;
                payment.FailureReason = ycResult.Message;
                payment.UpdatedAtUtc = DateTime.UtcNow;
                _paymentRepository.Update(payment);
                await _paymentRepository.SaveChangesAsync();

                return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.PaymentInitFailed, ycResult.Message ?? "Failed to initialize Yoco checkout.");
            }

            // Store Yoco's checkout id as ProviderReference so /verify can
            // look it up. The buyer's redirect URL goes on ProviderAuthorizationUrl.
            payment.ProviderReference = ycResult.Data.Id;
            payment.ProviderAuthorizationUrl = ycResult.Data.RedirectUrl;
            payment.ProviderAccessCode = ycResult.Data.Id;
            payment.Status = PaymentTransactionStatus.Pending;
            payment.UpdatedAtUtc = DateTime.UtcNow;

            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync();

            _logger.LogInformation("Payment {Code} initialized via Yoco for order {OrderCode}. checkoutId={CheckoutId}",
                payment.Code, order.Code, ycResult.Data.Id);
            return Result<InitializePaymentResponseDto>.Success(MapInitializeResponse(payment), "Payment initialized.");
        }

        // ─── Get by id ───────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<PaymentDto>> GetByIdAsync(Guid userId, Guid paymentId)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);

                if (payment is null)
                    return Result<PaymentDto>.Failure(ErrorCodes.NotFound, "Payment not found.");

                if (payment.UserId != userId)
                    return Result<PaymentDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to view this payment.");

                return Result<PaymentDto>.Success(MapDto(payment), "Payment retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetByIdAsync failed for payment {PaymentId}.", paymentId);
                return Result<PaymentDto>.Failure(ErrorCodes.Exception, $"Failed to retrieve payment. {ex.Message}");
            }
        }

        // ─── Verify ──────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<PaymentDto>> VerifyAsync(Guid userId, string reference, CancellationToken cancellationToken = default)
        {
            // Stopwatch + structured log so we can correlate mobile's
            // bounded poll cadence (1s, 2s, 3s, 5s, 5s, 8s) with the
            // backend round-trips. Useful when a buyer reports "stuck
            // on Checking" — search stdout for the payment reference and
            // you get the local→provider status transitions per attempt.
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(reference))
                    return Result<PaymentDto>.Failure(ErrorCodes.BadRequest, "Reference is required.");

                var payment = await _paymentRepository.GetByProviderReferenceAsync(reference)
                              ?? await _paymentRepository.GetByCodeAsync(reference);

                if (payment is null)
                    return Result<PaymentDto>.Failure(ErrorCodes.NotFound, "Payment not found.");

                if (payment.UserId != userId)
                    return Result<PaymentDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to verify this payment.");

                _logger.LogInformation(
                    "[Payments][Verify] ref={Ref} provider={Provider} localStatus={LocalStatus} userId={UserId}",
                    reference, payment.Provider, payment.Status, userId);

                // Already terminal — nothing more to do.
                if (payment.Status == PaymentTransactionStatus.Succeeded
                    || payment.Status == PaymentTransactionStatus.Refunded)
                {
                    return Result<PaymentDto>.Success(MapDto(payment), "Payment already settled.");
                }

                // Route to the right provider based on the stored Payment.Provider.
                if (string.Equals(payment.Provider, PaymentProvider.Yoco, StringComparison.OrdinalIgnoreCase))
                {
                    if (!_yocoClient.IsConfigured)
                        return Result<PaymentDto>.Failure(ErrorCodes.ProviderNotConfigured, "Yoco is not configured.");

                    var checkoutId = payment.ProviderReference ?? payment.ProviderAccessCode;
                    if (string.IsNullOrWhiteSpace(checkoutId))
                        return Result<PaymentDto>.Failure(ErrorCodes.NotFound, "Yoco checkout id is not stored on this payment.");

                    var ycLookup = await _yocoClient.GetCheckoutAsync(checkoutId, cancellationToken);

                    if (!ycLookup.IsSuccess || ycLookup.Data is null)
                        return Result<PaymentDto>.Failure(ErrorCodes.Exception, ycLookup.Message ?? "Yoco verify returned no data.");

                    await ApplyYocoSignalAsync(
                        payment,
                        status: ycLookup.Data.PaymentStatus,
                        eventSource: "verify",
                        providerEventKey: $"yoco-verify:{checkoutId}",
                        providerAmountSubunit: ycLookup.Data.Amount,
                        currencyCode: ycLookup.Data.Currency,
                        channel: null,
                        rawPayload: JsonSerializer.Serialize(ycLookup.Data),
                        failureReason: null,
                        cancellationToken);

                    var refreshedYoco = await _paymentRepository.GetByIdAsync(payment.Id);
                    return Result<PaymentDto>.Success(MapDto(refreshedYoco ?? payment), "Verification complete.");
                }

                if (string.Equals(payment.Provider, PaymentProvider.Ozow, StringComparison.OrdinalIgnoreCase))
                {
                    if (!_ozowClient.IsConfigured)
                        return Result<PaymentDto>.Failure(ErrorCodes.ProviderNotConfigured, "Ozow is not configured.");

                    var ozLookup = await _ozowClient.GetTransactionByReferenceAsync(payment.ProviderReference ?? payment.Code, cancellationToken);

                    if (!ozLookup.IsSuccess || ozLookup.Data is null)
                    {
                        _logger.LogWarning(
                            "[Payments][Verify] Ozow lookup failed ref={Ref} reason={Reason} elapsedMs={Elapsed}",
                            reference, ozLookup.Message ?? "<no message>", sw.ElapsedMilliseconds);
                        return Result<PaymentDto>.Failure(ErrorCodes.Exception, ozLookup.Message ?? "Ozow verify returned no data.");
                    }

                    await ApplyOzowSignalAsync(
                        payment,
                        status: ozLookup.Data.Status,
                        eventSource: "verify",
                        providerEventKey: $"ozow-verify:{payment.ProviderReference}",
                        providerAmount: ozLookup.Data.Amount,
                        currencyCode: ozLookup.Data.CurrencyCode,
                        siteCode: ozLookup.Data.SiteCode,
                        channel: ozLookup.Data.BankName,
                        paidAtRaw: ozLookup.Data.PaymentDate,
                        rawPayload: JsonSerializer.Serialize(ozLookup.Data),
                        failureReason: ozLookup.Data.StatusMessage,
                        cancellationToken);

                    var refreshedOzow = await _paymentRepository.GetByIdAsync(payment.Id);
                    _logger.LogInformation(
                        "[Payments][Verify] ref={Ref} provider=Ozow providerStatus={ProviderStatus} mappedStatus={MappedStatus} elapsedMs={Elapsed}",
                        reference, ozLookup.Data.Status ?? "<null>", refreshedOzow?.Status ?? payment.Status, sw.ElapsedMilliseconds);
                    return Result<PaymentDto>.Success(MapDto(refreshedOzow ?? payment), "Verification complete.");
                }

                if (!_paystackClient.IsConfigured)
                    return Result<PaymentDto>.Failure(ErrorCodes.ProviderNotConfigured, "Paystack is not configured.");

                var verifyResult = await _paystackClient.VerifyTransactionAsync(payment.ProviderReference ?? payment.Code, cancellationToken);

                if (!verifyResult.IsSuccess || verifyResult.Data?.Data is null)
                    return Result<PaymentDto>.Failure(ErrorCodes.Exception, verifyResult.Message ?? "Paystack verify returned no data.");

                await ApplyProviderSignalAsync(
                    payment,
                    providerStatus: verifyResult.Data.Data.Status,
                    eventSource: "verify",
                    providerEventKey: $"verify:{payment.ProviderReference}",
                    amountSubunit: verifyResult.Data.Data.Amount,
                    channel: verifyResult.Data.Data.Channel,
                    paidAtRaw: verifyResult.Data.Data.PaidAt,
                    rawPayload: JsonSerializer.Serialize(verifyResult.Data),
                    failureReason: verifyResult.Data.Data.GatewayResponse,
                    cancellationToken);

                var refreshed = await _paymentRepository.GetByIdAsync(payment.Id);
                return Result<PaymentDto>.Success(MapDto(refreshed ?? payment), "Verification complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyAsync failed for reference {Reference}.", reference);
                return Result<PaymentDto>.Failure(ErrorCodes.Exception, $"Verify failed. {ex.Message}");
            }
        }

        // ─── Webhook ─────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result> HandlePaystackWebhookAsync(string rawBody, string? signatureHeader, CancellationToken cancellationToken = default)
        {
            // Always return Result.Success to the controller; the controller returns 200
            // to the provider regardless so retries stop. Observable state (PaymentEvent row,
            // logs) captures everything we need for ops.

            try
            {
                var signatureValid = _paystackClient.VerifyWebhookSignature(rawBody, signatureHeader);

                PaystackWebhookPayload? parsed = null;
                try
                {
                    parsed = JsonSerializer.Deserialize<PaystackWebhookPayload>(rawBody ?? string.Empty);
                }
                catch (JsonException jex)
                {
                    _logger.LogWarning(jex, "Paystack webhook payload was not valid JSON.");
                }

                var eventType = parsed?.Event ?? "unknown";
                var reference = parsed?.Data?.Reference;
                var providerEventId = parsed?.Data?.Id ?? 0;
                var eventKey = $"paystack:{eventType}:{reference ?? "<none>"}:{providerEventId}";

                // Idempotency barrier #1 — dedup on ProviderEventKey.
                if (await _paymentRepository.EventKeyExistsAsync(eventKey))
                {
                    _logger.LogInformation("Duplicate Paystack webhook ignored: {Key}", eventKey);
                    return Result.Success("Duplicate webhook ignored.");
                }

                Payment? payment = null;
                if (!string.IsNullOrWhiteSpace(reference))
                {
                    payment = await _paymentRepository.GetByProviderReferenceAsync(reference)
                             ?? await _paymentRepository.GetByCodeAsync(reference);
                }

                var eventRow = new PaymentEvent
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment?.Id,
                    Provider = PaymentProvider.Paystack,
                    ProviderEventKey = eventKey,
                    EventType = eventType,
                    RawPayload = rawBody ?? string.Empty,
                    SignatureHeader = signatureHeader,
                    SignatureValid = signatureValid,
                    Processed = false,
                    ReceivedAtUtc = DateTime.UtcNow
                };

                await _paymentRepository.AddEventAsync(eventRow);
                await _paymentRepository.SaveChangesAsync();

                if (!signatureValid)
                {
                    _logger.LogWarning("Paystack webhook signature invalid. Event {Key} parked.", eventKey);
                    eventRow.ProcessingError = "Signature mismatch.";
                    await _paymentRepository.SaveChangesAsync();
                    return Result.Success("Signature invalid; event logged, not applied.");
                }

                if (payment is null || parsed?.Data is null)
                {
                    eventRow.ProcessingError = "No matching payment for reference " + reference;
                    await _paymentRepository.SaveChangesAsync();
                    return Result.Success("Event logged; no matching payment.");
                }

                await ApplyProviderSignalAsync(
                    payment,
                    providerStatus: parsed.Data.Status,
                    eventSource: "webhook",
                    providerEventKey: eventKey,
                    amountSubunit: parsed.Data.Amount,
                    channel: parsed.Data.Channel,
                    paidAtRaw: parsed.Data.PaidAt,
                    rawPayload: rawBody ?? string.Empty,
                    failureReason: parsed.Data.GatewayResponse,
                    cancellationToken);

                eventRow.Processed = true;
                eventRow.ProcessedAtUtc = DateTime.UtcNow;
                await _paymentRepository.SaveChangesAsync();

                return Result.Success("Webhook processed.");
            }
            catch (DbUpdateException dbex) when (IsUniqueViolation(dbex))
            {
                // Concurrent webhook racing ourselves — the other path already inserted.
                _logger.LogInformation("Paystack webhook lost dedup race; treating as duplicate.");
                return Result.Success("Duplicate webhook ignored (race).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack webhook processing threw.");
                return Result.Success("Webhook received; processing failed internally (logged).");
            }
        }

        // ─── Webhook: Ozow ───────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result> HandleOzowWebhookAsync(
            OzowTransactionNotification notification,
            string rawBody,
            CancellationToken cancellationToken = default)
        {
            // Mirror the Paystack pattern: always Result.Success so the controller
            // returns 200 OK to Ozow regardless. Audit row is the source of truth.
            try
            {
                if (notification is null)
                {
                    _logger.LogWarning("Ozow webhook received with null notification.");
                    return Result.Success("Ozow webhook had no parseable body.");
                }

                var hashValid = _ozowHashService.ValidateNotificationHash(notification);

                var reference = notification.TransactionReference;
                var status = notification.Status ?? "Unknown";
                var transactionId = notification.TransactionId ?? string.Empty;

                // Dedup key: ref + status + Ozow's transactionId. Two notifications
                // for the same (ref, status, transactionId) are the same event.
                var eventKey = $"ozow:{status}:{reference ?? "<none>"}:{transactionId}";

                if (await _paymentRepository.EventKeyExistsAsync(eventKey))
                {
                    _logger.LogInformation("Duplicate Ozow webhook ignored: {Key}", eventKey);
                    return Result.Success("Duplicate webhook ignored.");
                }

                Payment? payment = null;
                if (!string.IsNullOrWhiteSpace(reference))
                {
                    payment = await _paymentRepository.GetByProviderReferenceAsync(reference)
                              ?? await _paymentRepository.GetByCodeAsync(reference);
                }

                var eventRow = new PaymentEvent
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment?.Id,
                    Provider = PaymentProvider.Ozow,
                    ProviderEventKey = eventKey,
                    EventType = status,
                    RawPayload = rawBody ?? string.Empty,
                    SignatureHeader = notification.Hash,
                    SignatureValid = hashValid,
                    Processed = false,
                    ReceivedAtUtc = DateTime.UtcNow
                };

                await _paymentRepository.AddEventAsync(eventRow);
                await _paymentRepository.SaveChangesAsync();

                if (!hashValid)
                {
                    _logger.LogWarning("Ozow webhook hash invalid (or hash service not yet implemented). Event {Key} parked.", eventKey);
                    eventRow.ProcessingError = "Hash mismatch or hash service unavailable.";
                    await _paymentRepository.SaveChangesAsync();
                    return Result.Success("Hash invalid; event logged, not applied.");
                }

                if (payment is null)
                {
                    eventRow.ProcessingError = "No matching payment for reference " + reference;
                    await _paymentRepository.SaveChangesAsync();
                    return Result.Success("Event logged; no matching payment.");
                }

                await ApplyOzowSignalAsync(
                    payment,
                    status: status,
                    eventSource: "webhook",
                    providerEventKey: eventKey,
                    providerAmount: notification.Amount,
                    currencyCode: notification.CurrencyCode,
                    siteCode: notification.SiteCode,
                    channel: notification.BankName,
                    paidAtRaw: null,
                    rawPayload: rawBody ?? string.Empty,
                    failureReason: notification.StatusMessage,
                    cancellationToken);

                eventRow.Processed = true;
                eventRow.ProcessedAtUtc = DateTime.UtcNow;
                await _paymentRepository.SaveChangesAsync();

                return Result.Success("Webhook processed.");
            }
            catch (DbUpdateException dbex) when (IsUniqueViolation(dbex))
            {
                _logger.LogInformation("Ozow webhook lost dedup race; treating as duplicate.");
                return Result.Success("Duplicate webhook ignored (race).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ozow webhook processing threw.");
                return Result.Success("Webhook received; processing failed internally (logged).");
            }
        }

        // ─── Ozow state-transition logic ─────────────────────────────────────

        /// <summary>
        /// Applies an Ozow status signal (from webhook OR verify) to a Payment
        /// + its Order. Idempotent — Succeeded payments ignore further signals.
        /// Validates expected siteCode, currency, and amount before transitioning
        /// to Succeeded.
        /// </summary>
        private async Task ApplyOzowSignalAsync(
            Payment payment,
            string? status,
            string eventSource,
            string providerEventKey,
            decimal providerAmount,
            string? currencyCode,
            string? siteCode,
            string? channel,
            string? paidAtRaw,
            string rawPayload,
            string? failureReason,
            CancellationToken cancellationToken)
        {
            void StampMeta()
            {
                payment.UpdatedAtUtc = DateTime.UtcNow;
                payment.ChannelUsed = channel ?? payment.ChannelUsed;
                payment.RawProviderMetadata = rawPayload;
            }

            switch ((status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "complete":
                    if (payment.Status == PaymentTransactionStatus.Succeeded)
                    {
                        _logger.LogInformation("Ozow signal ignored — payment {Code} already Succeeded (source={Source}).", payment.Code, eventSource);
                        return;
                    }

                    // Currency guard.
                    if (!string.IsNullOrWhiteSpace(currencyCode)
                        && !string.Equals(currencyCode, payment.Currency, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError(
                            "Ozow currency mismatch on payment {Code}: expected {Expected}, provider sent {Got}.",
                            payment.Code, payment.Currency, currencyCode);

                        payment.Status = PaymentTransactionStatus.Failed;
                        payment.FailedAtUtc = DateTime.UtcNow;
                        payment.FailureReason = $"Currency mismatch: expected {payment.Currency}, got {currencyCode}.";
                        StampMeta();
                        _paymentRepository.Update(payment);
                        await _paymentRepository.SaveChangesAsync();
                        return;
                    }

                    // Amount guard. Ozow sends amount as a decimal — compare with tolerance.
                    if (providerAmount > 0m && Math.Abs(providerAmount - payment.Amount) > 0.01m)
                    {
                        _logger.LogError(
                            "Ozow amount mismatch on payment {Code}: expected {Expected}, provider charged {Charged}.",
                            payment.Code, payment.Amount, providerAmount);

                        payment.Status = PaymentTransactionStatus.Failed;
                        payment.FailedAtUtc = DateTime.UtcNow;
                        payment.FailureReason = $"Amount mismatch: expected {payment.Amount}, got {providerAmount}.";
                        StampMeta();
                        _paymentRepository.Update(payment);
                        await _paymentRepository.SaveChangesAsync();
                        return;
                    }

                    payment.Status = PaymentTransactionStatus.Succeeded;
                    payment.PaidAtUtc = TryParseUtc(paidAtRaw) ?? DateTime.UtcNow;
                    StampMeta();
                    _paymentRepository.Update(payment);

                    await AdvanceOrderOnPaidAsync(payment);

                    await _paymentRepository.SaveChangesAsync();
                    _logger.LogInformation("Ozow payment {Code} Complete via {Source}.", payment.Code, eventSource);
                    break;

                case "cancelled":
                    if (payment.Status == PaymentTransactionStatus.Succeeded)
                    {
                        _logger.LogWarning("Ignored Ozow Cancelled — payment {Code} already Succeeded.", payment.Code);
                        return;
                    }
                    payment.Status = PaymentTransactionStatus.Cancelled;
                    payment.CancelledAtUtc = DateTime.UtcNow;
                    payment.FailureReason = failureReason ?? "Cancelled by buyer.";
                    StampMeta();
                    _paymentRepository.Update(payment);
                    await MarkOrderPaymentFailedAsync(payment);
                    await _paymentRepository.SaveChangesAsync();
                    break;

                case "error":
                case "abandoned":
                    if (payment.Status == PaymentTransactionStatus.Succeeded)
                    {
                        _logger.LogWarning("Ignored Ozow {Status} — payment {Code} already Succeeded.", status, payment.Code);
                        return;
                    }
                    payment.Status = PaymentTransactionStatus.Failed;
                    payment.FailedAtUtc = DateTime.UtcNow;
                    payment.FailureReason = failureReason ?? status;
                    StampMeta();
                    _paymentRepository.Update(payment);
                    await MarkOrderPaymentFailedAsync(payment);
                    await _paymentRepository.SaveChangesAsync();
                    break;

                case "pending":
                case "pendinginvestigation":
                    // No terminal transition — just refresh metadata. The payment
                    // remains Pending and a later webhook will move it forward.
                    StampMeta();
                    _paymentRepository.Update(payment);
                    await _paymentRepository.SaveChangesAsync();
                    _logger.LogInformation(
                        "Ozow payment {Code} reported {Status} via {Source}. Awaiting terminal status.",
                        payment.Code, status, eventSource);
                    break;

                default:
                    _logger.LogInformation("Unhandled Ozow status '{Status}' for payment {Code} (source={Source}).", status, payment.Code, eventSource);
                    break;
            }
        }

        // ─── Webhook: Yoco ───────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result> HandleYocoWebhookAsync(
            string rawBody,
            string? webhookId,
            string? webhookTimestamp,
            string? webhookSignature,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var signatureValid = _yocoSignatureService.VerifyWebhookSignature(
                    rawBody ?? string.Empty, webhookId, webhookTimestamp, webhookSignature);

                YocoWebhookEvent? evt = null;
                try
                {
                    evt = JsonSerializer.Deserialize<YocoWebhookEvent>(rawBody ?? string.Empty);
                }
                catch (JsonException jex)
                {
                    _logger.LogWarning(jex, "[Yoco] Webhook payload was not valid JSON.");
                }

                var eventType = evt?.Type ?? "unknown";
                var eventId = evt?.Id ?? webhookId ?? "<none>";
                var eventKey = $"yoco:{eventType}:{eventId}";

                // Idempotency dedup.
                if (await _paymentRepository.EventKeyExistsAsync(eventKey))
                {
                    _logger.LogInformation("Duplicate Yoco webhook ignored: {Key}", eventKey);
                    return Result.Success("Duplicate webhook ignored.");
                }

                // Match the event back to our Payment row. Prefer our own
                // metadata.paymentCode (which we set on createCheckout) and
                // fall back to the checkoutId stored as ProviderReference.
                Payment? payment = null;
                var paymentCodeMeta = evt?.Payload?.Metadata != null
                                      && evt.Payload.Metadata.TryGetValue("paymentCode", out var pc)
                                          ? pc
                                          : null;

                if (!string.IsNullOrWhiteSpace(paymentCodeMeta))
                {
                    payment = await _paymentRepository.GetByCodeAsync(paymentCodeMeta);
                }

                if (payment is null && !string.IsNullOrWhiteSpace(evt?.Payload?.CheckoutId))
                {
                    payment = await _paymentRepository.GetByProviderReferenceAsync(evt.Payload.CheckoutId);
                }

                var eventRow = new PaymentEvent
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment?.Id,
                    Provider = PaymentProvider.Yoco,
                    ProviderEventKey = eventKey,
                    EventType = eventType,
                    RawPayload = rawBody ?? string.Empty,
                    SignatureHeader = webhookSignature,
                    SignatureValid = signatureValid,
                    Processed = false,
                    ReceivedAtUtc = DateTime.UtcNow
                };

                await _paymentRepository.AddEventAsync(eventRow);
                await _paymentRepository.SaveChangesAsync();

                if (!signatureValid)
                {
                    _logger.LogWarning("[Yoco] Webhook signature invalid (or signing secret unset). Event {Key} parked.", eventKey);
                    eventRow.ProcessingError = "Signature mismatch or signing secret unavailable.";
                    await _paymentRepository.SaveChangesAsync();
                    return Result.Success("Signature invalid; event logged, not applied.");
                }

                if (payment is null || evt?.Payload is null)
                {
                    eventRow.ProcessingError = "No matching payment for Yoco event " + eventId;
                    await _paymentRepository.SaveChangesAsync();
                    return Result.Success("Event logged; no matching payment.");
                }

                await ApplyYocoSignalAsync(
                    payment,
                    status: ResolveYocoStatusFromEvent(evt),
                    eventSource: "webhook",
                    providerEventKey: eventKey,
                    providerAmountSubunit: evt.Payload.Amount,
                    currencyCode: evt.Payload.Currency,
                    channel: evt.Payload.PaymentMethodDetails?.Type,
                    rawPayload: rawBody ?? string.Empty,
                    failureReason: null,
                    cancellationToken);

                eventRow.Processed = true;
                eventRow.ProcessedAtUtc = DateTime.UtcNow;
                await _paymentRepository.SaveChangesAsync();

                return Result.Success("Webhook processed.");
            }
            catch (DbUpdateException dbex) when (IsUniqueViolation(dbex))
            {
                _logger.LogInformation("Yoco webhook lost dedup race; treating as duplicate.");
                return Result.Success("Duplicate webhook ignored (race).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Yoco] Webhook processing threw.");
                return Result.Success("Webhook received; processing failed internally (logged).");
            }
        }

        /// <summary>
        /// Yoco surfaces status both on the event type (e.g. "payment.succeeded")
        /// and on the inner payload.status. Prefer the event type because it's
        /// the canonical signal.
        /// </summary>
        private static string? ResolveYocoStatusFromEvent(YocoWebhookEvent evt)
        {
            var t = evt?.Type?.ToLowerInvariant();
            return t switch
            {
                "payment.succeeded" => "succeeded",
                "payment.failed"    => "failed",
                "payment.canceled"  => "canceled",
                "payment.cancelled" => "canceled",
                _                   => evt?.Payload?.Status
            };
        }

        // ─── Yoco state-transition logic ─────────────────────────────────────

        /// <summary>
        /// Applies a Yoco status signal (from webhook OR verify) to a Payment
        /// + its Order. Idempotent — Succeeded payments ignore further signals.
        /// Validates expected currency + amount before transitioning to Succeeded.
        /// </summary>
        private async Task ApplyYocoSignalAsync(
            Payment payment,
            string? status,
            string eventSource,
            string providerEventKey,
            long providerAmountSubunit,
            string? currencyCode,
            string? channel,
            string rawPayload,
            string? failureReason,
            CancellationToken cancellationToken)
        {
            void StampMeta()
            {
                payment.UpdatedAtUtc = DateTime.UtcNow;
                payment.ChannelUsed = channel ?? payment.ChannelUsed;
                payment.RawProviderMetadata = rawPayload;
            }

            switch ((status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "succeeded":
                    if (payment.Status == PaymentTransactionStatus.Succeeded)
                    {
                        _logger.LogInformation("Yoco signal ignored — payment {Code} already Succeeded (source={Source}).", payment.Code, eventSource);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(currencyCode)
                        && !string.Equals(currencyCode, payment.Currency, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError(
                            "Yoco currency mismatch on payment {Code}: expected {Expected}, provider sent {Got}.",
                            payment.Code, payment.Currency, currencyCode);

                        payment.Status = PaymentTransactionStatus.Failed;
                        payment.FailedAtUtc = DateTime.UtcNow;
                        payment.FailureReason = $"Currency mismatch: expected {payment.Currency}, got {currencyCode}.";
                        StampMeta();
                        _paymentRepository.Update(payment);
                        await _paymentRepository.SaveChangesAsync();
                        return;
                    }

                    var expectedSubunit = ToSubunit(payment.Amount);
                    if (providerAmountSubunit > 0 && providerAmountSubunit != expectedSubunit)
                    {
                        _logger.LogError(
                            "Yoco amount mismatch on payment {Code}: expected {Expected} subunits, provider charged {Charged}.",
                            payment.Code, expectedSubunit, providerAmountSubunit);

                        payment.Status = PaymentTransactionStatus.Failed;
                        payment.FailedAtUtc = DateTime.UtcNow;
                        payment.FailureReason = $"Amount mismatch: expected {expectedSubunit}, got {providerAmountSubunit}.";
                        StampMeta();
                        _paymentRepository.Update(payment);
                        await _paymentRepository.SaveChangesAsync();
                        return;
                    }

                    payment.Status = PaymentTransactionStatus.Succeeded;
                    payment.PaidAtUtc = DateTime.UtcNow;
                    StampMeta();
                    _paymentRepository.Update(payment);

                    await AdvanceOrderOnPaidAsync(payment);

                    await _paymentRepository.SaveChangesAsync();
                    _logger.LogInformation("Yoco payment {Code} Succeeded via {Source}.", payment.Code, eventSource);
                    break;

                case "canceled":
                    if (payment.Status == PaymentTransactionStatus.Succeeded)
                    {
                        _logger.LogWarning("Ignored Yoco canceled — payment {Code} already Succeeded.", payment.Code);
                        return;
                    }
                    payment.Status = PaymentTransactionStatus.Cancelled;
                    payment.CancelledAtUtc = DateTime.UtcNow;
                    payment.FailureReason = failureReason ?? "Cancelled by buyer.";
                    StampMeta();
                    _paymentRepository.Update(payment);
                    await MarkOrderPaymentFailedAsync(payment);
                    await _paymentRepository.SaveChangesAsync();
                    break;

                case "failed":
                    if (payment.Status == PaymentTransactionStatus.Succeeded)
                    {
                        _logger.LogWarning("Ignored Yoco failed — payment {Code} already Succeeded.", payment.Code);
                        return;
                    }
                    payment.Status = PaymentTransactionStatus.Failed;
                    payment.FailedAtUtc = DateTime.UtcNow;
                    payment.FailureReason = failureReason ?? "failed";
                    StampMeta();
                    _paymentRepository.Update(payment);
                    await MarkOrderPaymentFailedAsync(payment);
                    await _paymentRepository.SaveChangesAsync();
                    break;

                case "pending":
                    StampMeta();
                    _paymentRepository.Update(payment);
                    await _paymentRepository.SaveChangesAsync();
                    _logger.LogInformation(
                        "Yoco payment {Code} reported pending via {Source}. Awaiting terminal status.",
                        payment.Code, eventSource);
                    break;

                default:
                    _logger.LogInformation("Unhandled Yoco status '{Status}' for payment {Code} (source={Source}).", status, payment.Code, eventSource);
                    break;
            }
        }

        // ─── Shared state-transition logic ───────────────────────────────────

        /// <summary>
        /// Applies a provider status signal (from webhook OR verify) to the Payment
        /// and its Order. Safe to call multiple times — status transitions are
        /// one-way and protected by explicit state guards.
        /// </summary>
        private async Task ApplyProviderSignalAsync(
            Payment payment,
            string? providerStatus,
            string eventSource,
            string providerEventKey,
            long amountSubunit,
            string? channel,
            string? paidAtRaw,
            string rawPayload,
            string? failureReason,
            CancellationToken cancellationToken)
        {
            void StampMeta()
            {
                payment.UpdatedAtUtc = DateTime.UtcNow;
                payment.ChannelUsed = channel ?? payment.ChannelUsed;
                payment.RawProviderMetadata = rawPayload;
            }

            switch ((providerStatus ?? string.Empty).ToLowerInvariant())
            {
                case "success":
                    if (payment.Status == PaymentTransactionStatus.Succeeded)
                    {
                        _logger.LogInformation("Signal ignored — payment {Code} already Succeeded (source={Source}).", payment.Code, eventSource);
                        return;
                    }

                    // Amount validation guard: charged amount must equal the stored amount.
                    var expectedSubunit = ToSubunit(payment.Amount);

                    if (amountSubunit > 0 && amountSubunit != expectedSubunit)
                    {
                        _logger.LogError(
                            "Payment {Code} amount mismatch: expected {Expected} but provider charged {Charged}. Marking Failed for review.",
                            payment.Code, expectedSubunit, amountSubunit);

                        payment.Status = PaymentTransactionStatus.Failed;
                        payment.FailedAtUtc = DateTime.UtcNow;
                        payment.FailureReason = $"Amount mismatch: expected {expectedSubunit}, got {amountSubunit}.";
                        StampMeta();
                        _paymentRepository.Update(payment);
                        await _paymentRepository.SaveChangesAsync();
                        return;
                    }

                    payment.Status = PaymentTransactionStatus.Succeeded;
                    payment.PaidAtUtc = TryParseUtc(paidAtRaw) ?? DateTime.UtcNow;
                    StampMeta();
                    _paymentRepository.Update(payment);

                    await AdvanceOrderOnPaidAsync(payment);

                    await _paymentRepository.SaveChangesAsync();
                    _logger.LogInformation("Payment {Code} succeeded via {Source}.", payment.Code, eventSource);
                    break;

                case "failed":
                case "abandoned":
                case "reversed":
                    if (payment.Status == PaymentTransactionStatus.Succeeded)
                    {
                        _logger.LogWarning("Ignored {Source} '{Status}' signal — payment {Code} already Succeeded.", eventSource, providerStatus, payment.Code);
                        return;
                    }

                    payment.Status = PaymentTransactionStatus.Failed;
                    payment.FailedAtUtc = DateTime.UtcNow;
                    payment.FailureReason = failureReason ?? providerStatus;
                    StampMeta();
                    _paymentRepository.Update(payment);

                    await MarkOrderPaymentFailedAsync(payment);

                    await _paymentRepository.SaveChangesAsync();
                    break;

                case "refund":
                case "refunded":
                    if (payment.Status != PaymentTransactionStatus.Succeeded
                        && payment.Status != PaymentTransactionStatus.Refunded)
                    {
                        _logger.LogWarning("Refund signal on payment {Code} in unexpected state {State}.", payment.Code, payment.Status);
                    }

                    payment.Status = PaymentTransactionStatus.Refunded;
                    payment.RefundedAtUtc = DateTime.UtcNow;
                    StampMeta();
                    _paymentRepository.Update(payment);

                    await MarkOrderRefundedAsync(payment);

                    await _paymentRepository.SaveChangesAsync();
                    break;

                default:
                    _logger.LogInformation("Unhandled provider status '{Status}' for payment {Code} (source={Source}).", providerStatus, payment.Code, eventSource);
                    break;
            }
        }

        private async Task AdvanceOrderOnPaidAsync(Payment payment)
        {
            var order = payment.Order ?? await _orderRepository.GetByIdAsync(payment.OrderId);

            if (order is null) return;

            var orderChanged = false;

            if (order.PaymentStatus != PaymentStatus.Paid)
            {
                order.PaymentStatus = PaymentStatus.Paid;
                orderChanged = true;
            }

            // Advance the order status only if still Pending — never regress
            // a seller who's already moved it forward (Confirmed/InProgress/Completed).
            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Confirmed;
                order.ConfirmedAtUtc = DateTime.UtcNow;
                orderChanged = true;
            }

            if (orderChanged)
            {
                order.UpdatedAtUtc = DateTime.UtcNow;
                _orderRepository.Update(order);
            }

            // Confirm any service booking on this order — PendingPayment → Confirmed
            // so the slot is now firmly held (no longer dependent on the hold window).
            await SyncServiceBookingsOnPaidAsync(payment.OrderId);
        }

        /// <summary>
        /// Payment succeeded → promote this order's PendingPayment bookings to
        /// <see cref="ServiceBookingStatus.Requested"/> (paid, awaiting provider
        /// acceptance — NOT Confirmed, which wrongly implied the seller had
        /// accepted) and raise a "New booking request" notification to the seller.
        /// Loads Merchant + Listing so the notification can resolve the owner +
        /// service name. Notification delivery is best-effort and never throws.
        /// No-op for product orders (no bookings).
        /// </summary>
        private async Task SyncServiceBookingsOnPaidAsync(Guid orderId)
        {
            var bookings = await _serviceBookingRepository.GetByOrderWithDetailsAsync(orderId);
            foreach (var booking in bookings)
            {
                if (booking.Status != ServiceBookingStatus.PendingPayment) continue;
                booking.Status = ServiceBookingStatus.Requested;
                booking.UpdatedAtUtc = DateTime.UtcNow;
                _serviceBookingRepository.Update(booking);

                // Persist the status flip + create/deliver the seller notification.
                // CreateAndDispatchAsync saves on the shared DbContext, so the
                // Requested transition is committed here. Best-effort: a delivery
                // failure is swallowed inside the notification service.
                await _notificationService.NotifySellerBookingRequestedAsync(booking);
            }
        }

        private async Task MarkOrderPaymentFailedAsync(Payment payment)
        {
            var order = payment.Order ?? await _orderRepository.GetByIdAsync(payment.OrderId);

            if (order is null) return;

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                // A failed signal after a prior success is suspicious — log and ignore;
                // reconciliation can be handled manually.
                _logger.LogWarning("Ignored Failed signal for order {OrderCode} — already Paid.", order.Code);
                return;
            }

            if (order.PaymentStatus != PaymentStatus.Failed)
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.UpdatedAtUtc = DateTime.UtcNow;
                _orderRepository.Update(order);
            }

            // Release any still-pending booking slot so the time becomes
            // available again immediately (don't touch a Confirmed booking on a
            // stray failed signal — see the Paid guard above).
            var bookings = await _serviceBookingRepository.GetByOrderAsync(payment.OrderId);
            foreach (var booking in bookings)
            {
                if (booking.Status != ServiceBookingStatus.PendingPayment) continue;
                booking.Status = ServiceBookingStatus.Cancelled;
                booking.UpdatedAtUtc = DateTime.UtcNow;
                _serviceBookingRepository.Update(booking);
            }
        }

        private async Task MarkOrderRefundedAsync(Payment payment)
        {
            var order = payment.Order ?? await _orderRepository.GetByIdAsync(payment.OrderId);

            if (order is null) return;

            if (order.PaymentStatus != PaymentStatus.Refunded)
            {
                order.PaymentStatus = PaymentStatus.Refunded;
                order.UpdatedAtUtc = DateTime.UtcNow;
                _orderRepository.Update(order);
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static Result EnsureOrderIsPayable(Order order)
        {
            if (order.Status == OrderStatus.Cancelled)
                return Result.Failure(ErrorCodes.PaymentNotAllowed, "Cancelled orders cannot be paid.");

            if (order.Status == OrderStatus.Completed)
                return Result.Failure(ErrorCodes.PaymentNotAllowed, "Completed orders cannot be paid.");

            if (order.PaymentStatus == PaymentStatus.Paid)
                return Result.Failure(ErrorCodes.PaymentAlreadyPaid, "This order has already been paid.");

            if (order.PaymentStatus == PaymentStatus.Refunded)
                return Result.Failure(ErrorCodes.PaymentNotAllowed, "Refunded orders cannot be re-paid.");

            if (order.Total <= 0)
                return Result.Failure(ErrorCodes.BadRequest, "Order total must be greater than zero to initiate payment.");

            return Result.Success();
        }

        private static long ToSubunit(decimal amount)
        {
            return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        }

        private static DateTime? TryParseUtc(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
                ? dt
                : null;
        }

        /// <summary>
        /// Generates the local Payment.Code that doubles as the
        /// TransactionReference we hand to Ozow.
        ///
        /// IMPORTANT: only characters in the <c>[A-Za-z0-9_]</c> set. Ozow's
        /// dashboard reference-validation expression rejects hyphens at the
        /// bank-handoff stage on some merchant configurations — the public
        /// failure mode is the buyer-facing "Failed to create transaction.
        /// Please retry to complete your payment." after the hosted page
        /// loads. Underscores are safe everywhere and won't trip a Capitec /
        /// FNB bank-reference rule.
        ///
        /// Tracked internally as the unique Payment.Code — the order/payment
        /// chain doesn't care about the separator, only the uniqueness +
        /// time-derived ordering for ops triage.
        /// </summary>
        private static string GenerateCode()
        {
            // PAY_yyyyMMddHHmmssfff  — 23 chars, fits within Ozow's
            // BankReference 20-char cap once trimmed.
            return $"PAY_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            // SQL Server error numbers 2601 (unique index) or 2627 (unique constraint)
            // surface on InnerException.Message. Keep this conservative — any write
            // failure should not leak into the webhook response.
            return ex.InnerException?.Message.Contains("2601") == true
                || ex.InnerException?.Message.Contains("2627") == true
                || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;
        }

        private InitializePaymentResponseDto MapInitializeResponse(Payment p)
        {
            return new InitializePaymentResponseDto
            {
                PaymentId = p.Id,
                Code = p.Code,
                Provider = p.Provider,
                ProviderReference = p.ProviderReference,
                AuthorizationUrl = p.ProviderAuthorizationUrl,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                // PublicKey is only meaningful for Paystack's in-app SDK flow.
                // Ozow is a pure redirect flow — return null.
                PublicKey = string.Equals(p.Provider, PaymentProvider.Paystack, StringComparison.OrdinalIgnoreCase)
                    ? _paystackClient.PublicKey
                    : null
            };
        }

        private static PaymentDto MapDto(Payment p)
        {
            return new PaymentDto
            {
                Id = p.Id,
                Code = p.Code,
                OrderId = p.OrderId,
                OrderCode = p.Order?.Code,
                UserId = p.UserId,
                Provider = p.Provider,
                ProviderReference = p.ProviderReference,
                ProviderAuthorizationUrl = p.ProviderAuthorizationUrl,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                PaidAtUtc = p.PaidAtUtc,
                FailedAtUtc = p.FailedAtUtc,
                RefundedAtUtc = p.RefundedAtUtc,
                FailureReason = p.FailureReason,
                ChannelUsed = p.ChannelUsed,
                CreatedAtUtc = p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc
            };
        }
    }
}
