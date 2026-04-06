using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZansiHustle.Application.Support.Dtos
{
    /// <summary>
    /// Request DTO for contact form submission.
    /// </summary>
    public class ContactRequestDto
    {
        /// <summary>
        /// Sender's full name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Sender's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Message subject (optional).
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Message content.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
