using System;

namespace ZansiHustle.Application.Payments.External.Dtos
{
    /// <summary>
    /// FROZEN wire contract — success body of POST /external-payments/sessions.
    /// Both fields are required by ZansiTech; a missing redirectUrl means the
    /// caller must treat payment initiation as failed.
    /// </summary>
    public sealed class CreateExternalPaymentSessionResponseDto
    {
        public Guid SessionId { get; set; }
        public string RedirectUrl { get; set; } = string.Empty;
    }
}
