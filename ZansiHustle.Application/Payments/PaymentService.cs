using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Payments.Dtos;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Application.Persistence.Payments;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.Payments;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.Payments;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Payments
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IPaystackClient _paystackClient;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IPaymentRepository paymentRepository, IOrderRepository orderRepository, IPaystackClient paystackClient, UserManager<User> userManager, ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _paystackClient = paystackClient;
            _userManager = userManager;
            _logger = logger;
        }

        // ─── Initialize ──────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<InitializePaymentResponseDto>> InitializeAsync(Guid buyerUserId, InitializePaymentRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request is null || request.OrderId == Guid.Empty)
                    return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.BadRequest, "OrderId is required.");

                var order = await _orderRepository.GetByIdAsync(request.OrderId);

                if (order is null)
                    return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.NotFound, "Order not found.");

                if (order.BuyerUserId != buyerUserId)
                    return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to pay for this order.");

                var guard = EnsureOrderIsPayable(order);

                if (!guard.IsSuccess)
                    return Result<InitializePaymentResponseDto>.Failure(guard.Code, guard.Message);

                // Reuse any in-flight attempt — same caller, same order → same Paystack session.
                var existing = await _paymentRepository.GetActiveAttemptForOrderAsync(order.Id);

                if (existing != null)
                {
                    _logger.LogInformation("Reusing active payment {Code} for order {OrderCode}.", existing.Code, order.Code);
                    return Result<InitializePaymentResponseDto>.Success(MapInitializeResponse(existing), "Resumed pending payment.");
                }

                if (!_paystackClient.IsConfigured)
                    return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.ProviderNotConfigured, "Paystack is not configured on this environment.");

                var buyer = await _userManager.FindByIdAsync(buyerUserId.ToString());

                if (buyer is null || string.IsNullOrWhiteSpace(buyer.Email))
                    return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.BadRequest, "Buyer email is required for payment initialization.");

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
                    CallbackUrl = request.CallbackUrl,
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

                _logger.LogInformation("Payment {Code} initialized for order {OrderCode}.", payment.Code, order.Code);
                return Result<InitializePaymentResponseDto>.Success(MapInitializeResponse(payment), "Payment initialized.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment initialize failed for order {OrderId}.", request?.OrderId);
                return Result<InitializePaymentResponseDto>.Failure(ErrorCodes.Exception, $"Failed to initialize payment. {ex.Message}");
            }
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

                // Already terminal — nothing more to do.
                if (payment.Status == PaymentTransactionStatus.Succeeded
                    || payment.Status == PaymentTransactionStatus.Refunded)
                {
                    return Result<PaymentDto>.Success(MapDto(payment), "Payment already settled.");
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

        private static string GenerateCode()
        {
            return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
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
                PublicKey = _paystackClient.PublicKey
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
