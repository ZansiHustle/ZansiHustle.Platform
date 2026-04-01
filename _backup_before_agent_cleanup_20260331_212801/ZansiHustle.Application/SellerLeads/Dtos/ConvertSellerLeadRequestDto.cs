using System;

namespace ZansiHustle.Application.SellerLeads.Dtos
{
    /// <summary>
    /// Request model used to convert a seller lead into a live seller reference.
    /// </summary>
    public class ConvertSellerLeadRequestDto
    {
        public Guid ConvertedSellerId { get; set; }
    }
}
