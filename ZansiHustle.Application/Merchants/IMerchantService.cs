using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Merchants.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Merchants
{
    /// <summary>
    /// Service contract for merchant operations.
    /// </summary>
    public interface IMerchantService
    {
        Task<Result<List<MerchantDto>>> GetAllAsync();
        Task<Result<MerchantDto>> GetByIdAsync(Guid id);
        Task<Result<MerchantDto>> CreateAsync(CreateMerchantRequestDto request);
        Task<Result<MerchantDto>> UpdateAsync(Guid id, UpdateMerchantRequestDto request);
        Task<Result<MerchantDto>> VerifyKycAsync(Guid id);
        Task<Result<MerchantDto>> UpdatePayoutEligibilityAsync(Guid id, bool eligible);
        Task<Result> DeleteAsync(Guid id);
    }
}
