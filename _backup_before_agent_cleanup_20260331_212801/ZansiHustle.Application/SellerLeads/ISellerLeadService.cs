using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.SellerLeads.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.SellerLeads
{
    /// <summary>
    /// Service contract for seller lead operations.
    /// </summary>
    public interface ISellerLeadService
    {
        Task<Result<List<SellerLeadListItemDto>>> GetAllAsync();
        Task<Result<SellerLeadDetailsDto>> GetByIdAsync(Guid id);
        Task<Result<SellerLeadDetailsDto>> CreateAsync(CreateSellerLeadRequestDto request);
        Task<Result<SellerLeadDetailsDto>> UpdateAsync(Guid id, UpdateSellerLeadRequestDto request);
        Task<Result<SellerLeadDetailsDto>> ReviewAsync(Guid id, ReviewSellerLeadRequestDto request);
        Task<Result<SellerLeadDetailsDto>> VerifyAsync(Guid id, VerifySellerLeadRequestDto request);
        Task<Result<SellerLeadDetailsDto>> ConvertAsync(Guid id, ConvertSellerLeadRequestDto request);
        Task<Result> DeleteAsync(Guid id);
    }
}
