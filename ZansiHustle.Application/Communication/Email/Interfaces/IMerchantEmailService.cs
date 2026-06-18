using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communication.Email.Interfaces
{
    public interface IMerchantEmailService
    {
        Task<Result> SendLeadWelcomeEmailAsync(string toEmail, string firstName, string? businessName = null, CancellationToken cancellationToken = default);

        Task<Result> SendNewLeadNotificationAsync(string leadName, string phone, string? email, string? category, string? province, string? referrerName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the seller-approval welcome/compliance onboarding email. Applies
        /// CommunicationTestMode recipient overriding (non-security purpose).
        /// Best-effort: returns a failure Result on send error but never throws.
        /// </summary>
        Task<Result> SendSellerApprovalWelcomeEmailAsync(string toEmail, string? firstName, string? businessName, CancellationToken cancellationToken = default);
    }
}
