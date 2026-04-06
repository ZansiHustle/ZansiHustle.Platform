using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZansiHustle.Application.Support.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Support
{
    /// <summary>
    /// Service for handling customer support operations.
    /// </summary>
    public interface ISupportEmailService
    {
        /// <summary>
        /// Processes a contact form submission and sends email notifications.
        /// </summary>
        Task<Result> ProcessContactFormAsync(ContactRequestDto request);
    }
}
